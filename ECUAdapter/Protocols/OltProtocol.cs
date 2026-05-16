using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using EcuCommunication.Protocols.Requests;
using Helper;
using Helper.ProgressDialog;
using SerialPortEx;

namespace EcuCommunication.Protocols;

public sealed class OltProtocol: IDiagProtocol, INotifyPropertyChanged
{
    #region Communication settings
    [Category("Communication settings"), Browsable(false)]
    [DefaultValue("COM1")]
    public string PortName
    {
        get => _serialPort.PortName;
        set
        {
            if (_serialPort.PortName == value) return;
            
            _serialPort.PortName = value;
            DoPropertyChanged(new PropertyChangedEventArgs("PortName"));
        }
    }
    private EnBaundRate _baundRate;
    [Category("Communication settings"), Browsable(false)]
    [DefaultValue(10400)]
    public EnBaundRate BaundRate
    {
        get => _baundRate;
        set
        {
            if (_baundRate == value) return;
            
            _baundRate = value;
            DoPropertyChanged(new PropertyChangedEventArgs("BaundRate"));
        }
    }

    [Category("Communication settings"), Browsable(false)]
    [DefaultValue(0)]
    public int ReadFreq { get; set; }

    [Category("Communication settings"), Browsable(false)]
    [DefaultValue(100)]
    public int ReadTimeout
    {
        get => _serialPort.ReadTimeout;
        set
        {
            if (_serialPort.ReadTimeout == value) return;
            
            _serialPort.ReadTimeout = value;
            DoPropertyChanged(new PropertyChangedEventArgs("ReadTimeout"));
        }
    }

    [Category("Communication settings"), Browsable(false)]
    [DefaultValue(100)]
    public int WriteTimeout
    {
        get => _serialPort.WriteTimeout;
        set
        {
            if (_serialPort.WriteTimeout == value) return;
            _serialPort.WriteTimeout = value;
            DoPropertyChanged(new PropertyChangedEventArgs("WriteTimeout"));
        }
    }

    [Category("Communication settings"), Browsable(false)]
    [DefaultValue(false)]
    public bool TraceEnabled { get; set; }

    [Browsable(false)]
    public Queue<Request> Requests { get; }
    
    [Browsable(false)]
    public Request IdleRequest { get; set; }
    
    [Browsable(false)]
    public bool IsBusy { get; private set; }

    public readonly StringBuilder OperationLog = new();

    private readonly byte[] tempBuffer = new byte[1024];

    public bool InitProgress { get; private set; }

    #endregion

    private static readonly Request StartCommunication = new JRequest("8110F18103");
    private static readonly Request StopCommunication = new JRequest("8110F18204");

    private static readonly Request StartDiagnosticSessionLow = new JRequest("8310F110810A1F");
    private static readonly Request StartDiagnosticSessionMedium = new JRequest("8310F11081263B");
    private static readonly Request StartDiagnosticSessionHi = new JRequest("8310F11081394E");
    private static readonly Request StopDiagnosticSession = new JRequest("8110F120A2");

    private static readonly Request TesterPresent = new JRequest("8210F13E01C2");
    private static readonly Request SwitchToRam = new JRequest("8410F1300F0601CB");        
    private static readonly Request LongDiagRequest = new JRequest("8210F1210FB3");
    private static readonly Request ShortDiagRequest = new JRequest("8210F1210EB2");
    private static readonly ReadRamRequest ReadRamRequest = new();
    private static readonly WriteRamRequest WriteRamRequest = new();

    private static readonly ReadErrorDataRequest ReadErrorDataRequest = new();
    private static readonly Request ClearErrorRequest = new JRequest("8310F114FF0097");
      
    private static readonly OltDiagV1DataRequest OltDiagV1DataRequest = new();
    private static readonly OltDiagV3DataRequest OltDiagV3DataRequest = new();
    private static readonly J7esOltDiagDataRequest J7esOltDiagDataRequest = new();
    private static readonly Euro2DiagDataRequest Euro2DiagDataRequest = new();
    private static readonly Rus83DiagDataRequest Rus83DiagDataRequest = new();

    private static readonly Request StopCaptureRequest = new JRequest("8310F130710025");
    private static readonly StartCaptureRequest StartCaptureRequest = new();

    private readonly IoControlRequest _ioControlRequest = new();

    private readonly SafeSerialPort _serialPort;
    private readonly BackgroundWorker _readThread;

    private bool _connected;
    
    /// <summary>
    /// Состояние соединения
    /// </summary>
    [Browsable(false)]        
    public bool Connected
    {
        get => _connected;
        private set
        {
            if (_connected == value) return;
            _connected = value;

            if (_connected)
                DoConnect(EventArgs.Empty);
            else                
                DoDisconnect(EventArgs.Empty); 
               
            DoPropertyChanged(new PropertyChangedEventArgs("Connected"));
        }
    }

    private IDiagDataRequest _diagRequest;

    private DateTime _connectTime = DateTime.Now;
    private FileStream _file;
    private StreamWriter _writer;
    private readonly object _lockObject = new();

    [Browsable(false)]
    public OltProtocolVersion Version { get; set; }

    private bool _isOnline;
    private static long lastUpdateTime;
    private byte[] _ecuSn;
    private byte[] _ecuSnHashBuffer = new byte[240];

    public uint SWDigest { get; private set; }        

    public bool IsOnline
    {
        get => _isOnline;
        private set
        {
            if (_isOnline == value) return;
            
            _isOnline = value;
            DoPropertyChanged(new PropertyChangedEventArgs("IsOnline"));
        }
    }                    

    /// <summary>
    /// Получить текущие значения диагностических параметров
    /// </summary>
    /// <returns></returns>
    public DiagData GetDiagData() 
        => _diagRequest == null ? DiagData.Empty : _diagRequest.DiagData;

    public TimeSpan Time => _diagRequest == null 
        ? TimeSpan.Zero 
        : _diagRequest.DiagData.Time;

    public bool CalcEcuSn { get; set; }
    public string EcuSn { get; set; }        

    public bool IsSupportOnlineCorrection 
        => _connected && _isOnline && _diagRequest is { IsOnline: true };

    public bool IsEcuErrorFound
    {
        get;
        private set
        {
            if (field == value) return;
            field = value;
            DoPropertyChanged(new PropertyChangedEventArgs("IsEcuErrorFound"));
        }
    }

    /// <summary>
    /// Событие соединение установлено
    /// </summary>
    public event EventHandler OnConnect;
    
    /// <summary>
    /// Событие соединение разорвано
    /// </summary>
    public event EventHandler OnDisconnect;

    #region Init
        
    public OltProtocol()
    {
        Version = OltProtocolVersion.OltDiagV1;
        _diagRequest = OltDiagV1DataRequest;
        ReadFreq = 0;
        Requests = new Queue<Request>();
        _readThread = new BackgroundWorker {WorkerSupportsCancellation = true};
        _readThread.DoWork += readThread_DoWork;
        _serialPort = new SafeSerialPort("COM1", 10400, Parity.None, 8, StopBits.One)
        {
            WriteTimeout = 100,
            ReadTimeout = 100
        };

        _ecuSn = new byte[8];
        _ecuSnHashBuffer = DiagProtocolHelper.InitEcuSnHashBuffer(_ecuSn, 240);
    }
    #endregion

    private void SetBaundRate()
    {
        _serialPort.BaudRate = _baundRate switch
        {
            EnBaundRate.Medium => 38400,
            EnBaundRate.Hi => 57600,
            EnBaundRate.Low => 10400,
            _ => throw new NotSupportedException()
        };

        _serialPort.DiscardInBuffer();
        _serialPort.DiscardOutBuffer();

        AddOperationLog($"Change ECU serial port baud rate: {_serialPort.BaudRate}");
    }

    private static void DataReceivedHandler(
        object sender,
        SerialDataReceivedEventArgs e)
    {
        lastUpdateTime = DateTime.Now.AddSeconds(10).Ticks;
        //Console.WriteLine("data received:"+ DateTime.Now );
    }
    /// <summary>
    /// Запуск опроса ЭБУ
    /// </summary>
    /// <returns></returns>
    public bool Start()
    {
        IsBusy = true;
        lastUpdateTime = DateTime.Now.AddSeconds(5).Ticks;
        try
        {
            _serialPort.PortName = PortName;
            _serialPort.BaudRate = 10400;
            _serialPort.Open();
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();
            _serialPort.DataReceived += DataReceivedHandler;
            AddOperationLog($"Open ECU serial port: {PortName}");

            AddOperationLog("Send start communication...");
            ExecuteRequest(StartCommunication);
            if (!StartCommunication.Test())
            {
                AddOperationLog("Error start communication");
                _serialPort.Close();                    
                return false;
            }

            var startDiag = _baundRate switch
            {
                EnBaundRate.Medium => StartDiagnosticSessionMedium,
                EnBaundRate.Hi => StartDiagnosticSessionHi,
                _ => StartDiagnosticSessionLow
            };

            AddOperationLog("Send start diagnostic session ...");
            ExecuteRequest(startDiag);
            
            if (!startDiag.Test())
            {
                AddOperationLog("Error diagnostic session");
                _serialPort.Close();                    
                return false;
            }

            if (_baundRate != EnBaundRate.Low)
                SetBaundRate();

            AddOperationLog("Send tester present ...");
            ExecuteRequest(TesterPresent);

            switch (Version)
            {
                case OltProtocolVersion.OltDiagV1:
                    AddOperationLog("Test new diag protocol support ...");
                    ExecuteRequest(J7esOltDiagDataRequest);
                    _diagRequest = J7esOltDiagDataRequest.Valid ? J7esOltDiagDataRequest : OltDiagV1DataRequest;
                    break;

                case OltProtocolVersion.OltDiagV3:
                    _diagRequest = OltDiagV3DataRequest;
                    break;

                case OltProtocolVersion.Euro2:
                    _diagRequest = Euro2DiagDataRequest;
                    break;

                case OltProtocolVersion.Rus83:
                    _diagRequest = Rus83DiagDataRequest;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            IdleRequest = (Request)_diagRequest;
            Connected = true;

            _readThread.RunWorkerAsync();

            return true;
        }
           
        finally
        {
            IsBusy = false;
        }
    }

    private void AddOperationLog(string line) => OperationLog.AppendLine(line);

    /// <summary>
    /// Окончание опроса ЭБУ
    /// </summary>
    public void Stop()
    {
        IsBusy = true;

        try
        {
            IdleRequest = TesterPresent;
            _readThread.CancelAsync();
            BackgroundWorkerHelper.Wait(_readThread);
            IdleRequest = null;
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();
            AddOperationLog("Send stop diagnostic session ...");
            ExecuteRequest(StopDiagnosticSession);
            
            if (_baundRate != EnBaundRate.Low)
                _serialPort.BaudRate = 10400;
            
            AddOperationLog("Send stop communication...");
            ExecuteRequest(StopCommunication);
        }
        finally
        {
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();
            _serialPort.Close();
            Connected = false;
            IsOnline = false;
            IsBusy = false;
        }                       
    }       

    public void WriteRam(int address, byte[] source, IProgress progress = null, bool async = false)
    {
        var idleReguest = IdleRequest;
        IdleRequest = null;

        try
        {
            const int size = 0x60;
            if (source.Length <= size)
            {
                InnerWriteRam(address, source, async);
                return;
            }

            var buffer = new byte[size];
            var count = (int) Math.Ceiling((float) source.Length/size);
            var index = 0;

            for (var i = 0; i < count; i++)
            {
                if (progress is { Cancel: true }) return;

                var writeSize = Math.Min(size, source.Length - index);
                if (writeSize != size)
                    Array.Resize(ref buffer, writeSize);

                Buffer.BlockCopy(source, index, buffer, 0, writeSize);
                InnerWriteRam(address, buffer, async);
                address += writeSize;
                index += writeSize;

                progress?.IterationComplete(this, i, count);
            }
        }
        finally
        {
            IdleRequest = idleReguest;
        }
    }

    private void InnerWriteRam(int address, byte[] source, bool async = false)
    {
        if (async)
        {
            var writeRam = new WriteRamRequest();
            writeRam.Prepare(address, address < 0xF400 ? DataHelper.Xor(source, _ecuSnHashBuffer) : source);
            Requests.Enqueue(writeRam);
        }
        else
        {
            WriteRamRequest.Prepare(address, address < 0xF400 ? DataHelper.Xor(source, _ecuSnHashBuffer) : source);
            ExecuteRequest(WriteRamRequest);
        }
    }

    public void WriteFirmware(IWin32Window owner, byte[] source)
    {
        const int address = 0x5EF0;
        const int count = 0xB030 - address;
        var buffer = new byte[count];

        Buffer.BlockCopy(source, address, buffer, 0, count);
        using var progress = ProgressForm.ShowProgress(owner);
        WriteRam(address, buffer, progress);
        progress.Close();
    }       

    private void readThread_DoWork(object sender, DoWorkEventArgs e)
    {
        InitProgress = true;            
        var online = TestOnline();

        if (online)
            PrepareOnline();

        IsOnline = online;
        InitProgress = false;

        while (true)
        {
            if (_readThread.CancellationPending) break;

            if (Requests.Count > 0)
            {
                var request = Requests.Dequeue();
                ExecuteRequest(request);
            }
            else
            {
                ExecuteRequest(IdleRequest);
                if (IdleRequest is { Valid: true })
                {
                    _diagRequest.DiagData.Time = DateTime.Now - _connectTime;
                    IsEcuErrorFound = _diagRequest.DiagData.IsErrorFound;
                    DoDiagUpdate(EventArgs.Empty);
                }
            }

            if (_readThread.CancellationPending) break;
            if (ReadFreq > 0) Thread.Sleep(ReadFreq);
        }
    }        

    private bool TestOnline()
    {
        AddOperationLog("Switch to RAM ...");
        ExecuteRequest(SwitchToRam);
        if (!SwitchToRam.Test())
        {
            AddOperationLog("Error switch to RAM");
            return false;
        }                                   

        AddOperationLog("Test long diagnostic request ...");
        ExecuteRequest(LongDiagRequest);
        if (!LongDiagRequest.Test())
        {
            AddOperationLog("Not supported long diagnostic request");
            return false;
        }

        AddOperationLog("Test short diagnostic request ...");
        ExecuteRequest(ShortDiagRequest);
        if (!ShortDiagRequest.Test())
        {
            AddOperationLog("Not supported short diagnostic request");
            return false;
        }

        ReadRamRequest.Prepare(0xF300, 8);
        ExecuteRequest(ReadRamRequest);
        if (!ReadRamRequest.Valid)
        {
            AddOperationLog("Error read 0xF300(8)");
            return false;
        }
        AddOperationLog("Read 0xF300(8): " + DataHelper.ByteArrayToStr(ReadRamRequest.value));
        
        //TODO: Определение типа ЭБУ (онлайн или нет)
        var online = !DataHelper.Compare(ReadRamRequest.value, [0, 1, 2, 3, 4, 5, 6, 7]);

        AddOperationLog(online ? "is online ECU" : "is no online ECU");

        return online;
    }

    private void PrepareOnline()
    {
        SWDigest = 0;                    

        ReadRamRequest.Prepare(0xF3F8, 8);
        ExecuteRequest(ReadRamRequest);
        AddOperationLog("Read 0xF3F8(8): " + DataHelper.ByteArrayToStr(ReadRamRequest.value));

        ReadRamRequest.Prepare(0xF6F8, 8);
        ExecuteRequest(ReadRamRequest);
        AddOperationLog("Read 0xF6F8(8): " + DataHelper.ByteArrayToStr(ReadRamRequest.value)); 
           
        ReadRamRequest.Prepare(0xC0, 16);
        ExecuteRequest(ReadRamRequest);
        AddOperationLog("Read 0xC0(16): " + DataHelper.ByteArrayToStr(ReadRamRequest.value));

        if (CalcEcuSn && ReadRamRequest.value != null)
        {
            _ecuSn = DiagProtocolHelper.CalcEcuSn(ReadRamRequest.value);
        }
        else
        {
            _ecuSn = DataHelper.StrToByteArray(EcuSn, 8);                
        }

        _ecuSnHashBuffer = DiagProtocolHelper.InitEcuSnHashBuffer(_ecuSn, 240);
        AddOperationLog("ECU SN: " + DataHelper.ByteArrayToStr(_ecuSn));

        var index = 0;
        
        //TODO: чтение 176 байт 30h + 30h + 30h + 20h
        var buffer = new byte[0xB0];
        
        for (var i = 0; i < 3; i++)
            if (!ReadRamPart(buffer, 0x30, ref index)) return;

        if (!ReadRamPart(buffer, 0x20, ref index)) return;

        //AddOperationLog("SWDigest buffer: " + DataHelper.ByteArrayToStr(buffer));
        SWDigest = DataHelper.CalculateCRC(buffer, 0, buffer.Length);
        AddOperationLog("SWDigest: " + SWDigest.ToString("X4"));            
    }

    private bool ReadRamPart(byte[] buffer, int count, ref int address)
    {
        ReadRamRequest.Prepare(address, count);
        ExecuteRequest(ReadRamRequest);
        if (!ReadRamRequest.Valid) 
            return false;
        if (address < 0xF300)
            ReadRamRequest.value = DataHelper.Xor(ReadRamRequest.value, _ecuSnHashBuffer);
        Buffer.BlockCopy(ReadRamRequest.value, 0, buffer, address, count);
        address += count;
        return true;
    }

    /// <summary>
    /// Выполнить запрос к ЭБУ
    /// </summary>
    /// <param name="request"></param>
    public void ExecuteRequest(Request request)
    {
        if (request == null || !_serialPort.IsOpen) return;
        
        lock (_lockObject)
        {                
            var buffer = request.GetRequest();
            byte[] reply = null;

            try
            {                    
                if (buffer == null || buffer.Length == 0) return;

                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();

                try
                {
                    _serialPort.Write(buffer, 0, buffer.Length);
                }
                catch (TimeoutException)
                {
                    return;
                }

                var offset = 0;

                do
                {
                    int count;
                    try
                    {
                        count = _serialPort.Read(tempBuffer, offset, 100);
                    }
                    catch (TimeoutException)
                    {                            
                        break;
                    }

                    offset += count;                        
                } while (true);

                reply = new byte[offset];
                Buffer.BlockCopy(tempBuffer, 0, reply, 0, offset);                    

                request.SetReply(ref reply);
            }
            catch
            {
                // ignored
            }
            finally
            {
                if (lastUpdateTime < DateTime.Now.Ticks) {
                    //Connected = false;
                    //IsBusy = false;
                    //Stop();
                }
                if (TraceEnabled)
                {
                    var source = $"{DataHelper.ByteArrayToStr(buffer)}\t\t->\t{DataHelper.ByteArrayToStr(reply)}";
                    
                    WriteTrace(source);
                }
            }
        }
    }

    private void WriteTrace(string source)
    {
        if (_file == null)
            InitTraceFile();

        _writer.WriteLine(source);
    }

    private void InitTraceFile()
    {
        var basePath = Application.StartupPath + @"\trace\";
        if (!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);

        var fileName = $"ecu_trace_{DateTime.Now.ToString("yyyy-MM-dd")}_{DateTime.Now.ToString("HH-mm-ss")}.txt";
        var filePath = basePath + fileName;
        _file = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(_file);            
    }

    private void CloseTrace()
    {
        if (_file == null) return;
        _writer.Flush();
        _writer.Close();
        _writer = null;

        _file.Close();
        _file = null;
    }

    private void DoConnect(EventArgs e)
    {
        _connectTime = DateTime.Now - Time;
        var connect = OnConnect;
        
        connect?.Invoke(this, e);
    }

    private void DoDisconnect(EventArgs e)
    {
        CloseTrace();
        var disconnect = OnDisconnect;
        
        disconnect?.Invoke(this, e);
    }

    #region Implementation of IDiagProtocol

    /// <summary>
    /// Событие обновились диагностические данные
    /// </summary>
    public event EventHandler OnDiagUpdate;

    private void DoDiagUpdate(EventArgs e)
    {
        var du = OnDiagUpdate;
        du?.Invoke(this, e);
    }        

    #endregion

    #region Implementation of INotifyPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    private void DoPropertyChanged(PropertyChangedEventArgs e)
    {
        var pc = PropertyChanged;
        pc?.Invoke(this, e);
    }

    #endregion

    public void ClearErrors()
    {
        if (!Connected) return;
        Requests.Enqueue(ClearErrorRequest);
    }

    public List<ECUError> ReadErrors()
    {
        if (!Connected) return [];
        
        ExecuteRequest(ReadErrorDataRequest);
        return ReadErrorDataRequest.errors;
    }

    public void StopCapture() => ExecuteRequest(StopCaptureRequest);

    public void StartCapture(byte captureId)
    {
        StartCaptureRequest.Prepare(captureId);
        ExecuteRequest(StartCaptureRequest);
    }

    public bool StartIoControl(IoControlType ioControlType, byte rawValue)
    {
        _ioControlRequest.StartCaptureAndSetValue((byte) ioControlType, rawValue);
        ExecuteRequest(_ioControlRequest);
        return _ioControlRequest.Test();
    }

    public void StopIoControl(IoControlType ioControlType)
    {
        _ioControlRequest.StopCapture((byte)ioControlType);
        ExecuteRequest(_ioControlRequest);
    }

    public void SetIoControlValue(IoControlType ioControlType, byte rawValue)
    {
        var request = new IoControlRequest();
        request.StartCaptureAndSetValue((byte)ioControlType, rawValue);
        Requests.Enqueue(request);
    }
}