using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Threading;
using Helper;

namespace KWPTest;

class LC1Emul: IDisposable
{
    private readonly SerialPort _serialPort;       
    private readonly byte[] _data;
    private readonly BackgroundWorker sendThread;
    internal readonly List<string> TraceLines = [];

    public LC1Emul()
    {
        _serialPort = new SerialPort("COM6", 19200, Parity.None, 8, StopBits.One) {ReadTimeout = 50, WriteTimeout = 90};            
        _data = new byte[6];
        InitDataBuffer();

        sendThread = new BackgroundWorker {WorkerSupportsCancellation = true};
        sendThread.DoWork += SendThread_DoWork;            
    }

    private bool _o2Level;
    private int _traceIndex;

    public bool O2Level
    {
        get => _o2Level;
        set
        {
            _o2Level = value;
            InitDataBuffer();
        }
    }

    void SendThread_DoWork(object sender, DoWorkEventArgs e)
    {
        while (true)
        {
            if (sendThread.CancellationPending) return;
            SendData();
        }
    }

    private void InitDataBuffer()
    {
        _data[0] = 0xB2; //10110010   header hi byte
        _data[1] = 0x82; //10000010   header lo byte

        if (_o2Level)
        {
            _data[2] = 0x46; //01000010  //O2 level
            _data[3] = 0x31; //AFR
        }
        else
        {
            _data[2] = 0x43; //01000010 
            _data[3] = 0x13; //AFR
        }            
            
        SetAFR(14.7f);
    }

    public void Start(string serialPortName)
    {
        if (_serialPort.PortName != serialPortName)
        {            
            _serialPort.Close();
            _serialPort.PortName = serialPortName;
        }

        if (!_serialPort.IsOpen)
        {
            _serialPort.Open();
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();
        }

        if (!sendThread.IsBusy && _serialPort.IsOpen)
            sendThread.RunWorkerAsync();            
    }

    public void Stop()
    {
        sendThread.CancelAsync();
        BackgroundWorkerHelper.Wait(sendThread);
        _serialPort.Close();
    }

    public void SetAFR(float afr)
    {
        var lambda = (ushort)(O2Level ? afr * 10 : (afr / 14.7f - 0.5f) * 1000f);
        _data[4] = (byte)((lambda >> 7) & 0x7F);
        _data[5] = (byte)(lambda & 0x7F);
    }

    private void SendData()
    {
        if (!_serialPort.IsOpen) return;

        if (TestReadBuffer()) return;

        if (TraceLines.Count > 0)
        {
            var traceLine = DataHelper.StrToByteArray(TraceLines[_traceIndex]);
            for (var i = 0; i < traceLine.Length; i++)
            {
                try
                {
                    _serialPort.Write(traceLine, i, 1);
                }
                catch (TimeoutException)
                { }
            }

            _traceIndex = (_traceIndex + 1)%TraceLines.Count;
            Thread.Sleep(100);
        }
        else
        {
            for (var i = 0; i < _data.Length; i++)
            {
                try
                {
                    _serialPort.Write(_data, i, 1);
                }
                catch (TimeoutException)
                { }
            }

            Thread.Sleep(100);
        }
    }

    private bool TestReadBuffer()
    {
        var count = _serialPort.BytesToRead;
        if (count <= 0) return false;
        
        byte[] buffer;
        var requestBuffer = new List<byte>();
        do
        {
            buffer = new byte[count];
            _serialPort.Read(buffer, 0, count);
            requestBuffer.AddRange(buffer);
            count = _serialPort.BytesToRead;
        } while (count > 0);

        buffer = requestBuffer.ToArray();
        
        var answer = buffer[0] switch
        {
            0xF3 => new byte[] { 0xA2, 0x85, 0x01, 0x73, 0x11, 0x0A, 0x4C, 0x43, 0x31, 0x20, 0x05, 0x32 },
            0xCE => new byte[] { 0xA2, 0x85, 0x01, 0x4E, 0x4C, 0x43, 0x2D, 0x31, 0x00, 0x00, 0x00, 0x00 },
            //Command ‘S’ - Get Setup Mode Header 
            0x53 => new byte[]
            {
                /*0xA2, 0x85, 0x01, 0x53,*/
                0x11, 0x0A, 0x4C, 0x43, 0x31, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            },
            _ => Array.Empty<byte>()
        };

        if (answer.Length <= 0) return false;
        
        try
        {
            _serialPort.Write(answer, 0, answer.Length);
        }                    
        catch (TimeoutException)
        { }
        
        return true;
    }

    #region Implementation of IDisposable

    public void Dispose()
    {
        Stop();
    }

    #endregion
}