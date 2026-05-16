using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CalibrTable;
using CtpMaps;
using CtpMaps.DataTypes;
using EcuCommunication.Protocols;
using Helper.ProgressDialog;
using OpenOltTypes;
using WidebandLambdaCommunication;
using DataHelper = Helper.DataHelper;

namespace OpenOLT.Firmware;

public class FirmwareManager: IFirmwareManager
{
    public event EventHandler<AutoCorrectionEventArgs> AutoCorrection;
 
    private const int FirmwareSize = 0x10000;
    private readonly OpenFileDialog _openFileDialog;
    public byte[] Buffer { get; } = new byte[FirmwareSize];

    public string Name { get; private set; }
    public string FilePath { get; private set; }
    public bool IsOpened { get; private set; }
    public bool IsFastRpm { get; private set; }
    public bool IsVolumetricEfficiency { get; set; }
    private int[] _rpmRt32 = new int[32];
    private int[] _rpmRt16 = new int[16];

    private readonly byte[] _thrSampling = new byte[101];
    public float minGbc;
    public float stepGbc;
    private int rpmRtIndex;
    private int rpmRt32Index;
    private int thrRtIndex;
    private int rpmThrRtIndex;
    private int gbcRtIndex;
    private int rpmGbcRtIndex;
    private int rpm32ThrRtIndex;
    private int twatRtIndex;
    private int rpmPressRtIndex;
    private int rpm32PressRtIndex;

    private bool isVE;
    private readonly OnlineManager onlineManager;
    private int stabylityCount;
    private int oldRpmThrRtIndex;

    public uint SWDigest { get; private set; }

    private FileStream firmwareFile;
    private float kmin, kmax;
    private byte[] _rpmSampling;
    public J7esFlags J7esFlags { get; }

    #region indexes
    public int TwatRtIndex
    {
        get => twatRtIndex;
        private set
        {
            if (twatRtIndex == value) return;
            twatRtIndex = value;
            OnPropertyChanged("TwatRtIndex");
        }
    }

    public int RpmRtIndex
    {
        get => rpmRtIndex;
        private set
        {
            if (rpmRtIndex == value) return;
            rpmRtIndex = value;
            OnPropertyChanged("RpmRtIndex");
        }
    }

    public int RpmRt32Index
    {
        get => rpmRt32Index;
        private set
        {
            if (rpmRt32Index == value) return;
            rpmRt32Index = value;
            OnPropertyChanged("RpmRt32Index");
        }
    }

    public int ThrRtIndex
    {
        get => thrRtIndex;
        private set
        {
            if (thrRtIndex == value) return;
            thrRtIndex = value;
            OnPropertyChanged("ThrRtIndex");
        }
    }

    public int RpmThrRtIndex
    {
        get => rpmThrRtIndex;
        private set
        {
            if (rpmThrRtIndex == value) return;
            rpmThrRtIndex = value;
            OnPropertyChanged("RpmThrRtIndex");
        }
    }

    public int GbcRtIndex
    {
        get => gbcRtIndex;
        private set
        {
            if (gbcRtIndex == value) return;
            gbcRtIndex = value;
            OnPropertyChanged("GbcRtIndex");
        }
    }

    public int RpmGbcRtIndex
    {
        get => rpmGbcRtIndex;
        private set
        {
            if (rpmGbcRtIndex == value) return;
            rpmGbcRtIndex = value;
            OnPropertyChanged("RpmGbcRtIndex");
        }
    }

    public int Rpm32ThrRtIndex
    {
        get => rpm32ThrRtIndex;
        private set
        {
            if (rpm32ThrRtIndex == value) return;
            rpm32ThrRtIndex = value;
            OnPropertyChanged("Rpm32ThrRtIndex");
        }
    }

    public int RpmPressRtIndex
    {
        get => rpmPressRtIndex;
        private set
        {
            if (rpmPressRtIndex == value) return;
            rpmPressRtIndex = value;
            OnPropertyChanged("RpmPressRtIndex");
        }
    }

    public int Rpm32PressRtIndex
    {
        get => rpm32PressRtIndex;
        private set
        {
            if (rpm32PressRtIndex == value) return;
            rpm32PressRtIndex = value;
            OnPropertyChanged("Rpm32PressRtIndex");
        }
    }
    public TableValues<byte, short> Gbc { get; private set; }

    public TableValues<byte, float> Kgbc { get; private set; }

    public TableValues<byte, float> Kgbc_press { get; private set; }

    public int[] RpmRt32 => _rpmRt32;

    public int[] RpmRt16 => _rpmRt16;

    public int[] TwatRt { get; } = new int[39];

    public int[] GbcRt { get; } = new int[16];

    public int[] ThrRt { get; } = new int[16];

    public int[] PressRt { get; } = new int[16];

    public int[] PressRt32 { get; } = new int[32];

    public float[] Rpm16_16RtPoints { get; } = new float[16 * 16];

    public float[] Rpm32_16RtPoints { get; } = new float[32 * 16];

    public float[] Rpm32_32RtPoints { get; } = new float[32 * 32];

    public float[] GbcRtPoints { get; } = new float[256];

    public float[] ThrRtPoints { get; } = new float[256];

    public float[] PressRtPoints { get; } = new float[256];

    public float[] PressRt32Points { get; } = new float[32*32];

    #endregion

    public event EventHandler<EventArgs> OnOpenFirmware;
    public event EventHandler<EventArgs> OnCloseFirmware;

    private void DoOpenFirmware(EventArgs e) 
        => OnOpenFirmware?.Invoke(this, e);

    private void DoCloseFirmware(EventArgs e) 
        => OnCloseFirmware?.Invoke(this, e);

    public FirmwareManager(OnlineManager onlineManager)
    {
        J7esFlags = new J7esFlags();
        this.onlineManager = onlineManager;
        isVE = false;
        Name = string.Empty;
        
        //var path = Application.StartupPath + @"\firmwares";
        _openFileDialog = new OpenFileDialog
        {
            Filter = "firmware files|*.bir;*.bin|all files|*.*"
        };
        
        kmin = 0.4f;
        kmax = 1.98f;
    }

    private void GBCOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "Table") return;
        var address = Gbc.Address;
        var tablebuffer = Gbc.GetRawBuffer();
        WriteRam(address, address, tablebuffer);
    }

    private void KGBOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "Table") return;
        
        var address = (isVE ? Kgbc_press : Kgbc).Address;
        var tablebuffer = (isVE ? Kgbc_press : Kgbc).GetRawBuffer();
        
        WriteRam(address, address, tablebuffer);
    }        

    public bool OpenDialog(IWin32Window owner)
    {                                    
        if (_openFileDialog.ShowDialog(owner) != DialogResult.OK) return false;

        var path = _openFileDialog.FileName;
            
        return Open(path, true, owner);
    }

    public bool Open(string path, bool showMessage = false, IWin32Window owner = null)
    {
        if (string.IsNullOrEmpty(path)) return false;
            
        var fileInfo = new FileInfo(path);
        if (!fileInfo.Exists)
        {
            if (showMessage)
                MessageBox.Show(owner, $"Файл прошивки {path} не найден");
            
            return false;
        }

        if (fileInfo.Length != FirmwareSize && !MapDataHelper.UnpackCtpFirmware(fileInfo, showMessage, owner))
            return false;

        OpenFirmwareFile(path);            
        return true;
    }

    public void Close()
    {
        CloseFirmwareFile();
    }

    private void OpenFirmwareFile(string path)
    {
        CloseFirmwareFile();

        firmwareFile = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        firmwareFile.Read(Buffer, 0, FirmwareSize);           
        FilePath = path;
        Name = Path.GetFileName(path);
        Prepare();
        IsOpened = true;

        onlineManager.settings.LastFirmwarePath = FilePath;

        DoOpenFirmware(EventArgs.Empty);
    }

    private void CloseFirmwareFile()
    {
        if (firmwareFile == null) return;
        
        firmwareFile.Flush(true);
        firmwareFile.Close();
        firmwareFile = null;
        DoCloseFirmware(EventArgs.Empty);
    }

    private void SaveToFile()
    {
        firmwareFile.Seek(0, SeekOrigin.Begin);
        firmwareFile.Write(Buffer, 0, FirmwareSize);
        firmwareFile.Flush();
    }

    private void Prepare()
    {
        SWDigest = DataHelper.CalculateCRC(Buffer, 0, 0xB0);            
        IsFastRpm = DataHelper.IndexOf(Buffer, [0x90, 0x61, 0x3C, 0xE5, 0x55]) != -1;

        J7esFlags.Prepare(Buffer);
        FillKGbc();
        if (J7esFlags.IsDadMode && !J7esFlags.IsCommonKGBCTable)
            Kgbc.Address = FirmwareHelper.KGbcJ7esDadAddr;

        FirmwareHelper.FillRpmRT(Buffer, out _rpmSampling, out _rpmRt32, out _rpmRt16);            
        FillThrRT();
        FillGbcRT();
        FillTWatRT();
        FillPressRT();

        FillPoints();

        FillGbc();
        FillKGbc();
        FillKGbcPress();            
    }

    private void FillPressRT()
    {
        var minPress = (Buffer[FirmwareHelper.MinPressAddr] | (Buffer[FirmwareHelper.MinPressAddr + 1] << 8))/6.0;
        var rangePress = Math.Round(82485.0 / (Buffer[FirmwareHelper.RangePressAddr] == 0 ? 1 : Buffer[FirmwareHelper.RangePressAddr]), 2, MidpointRounding.AwayFromZero);
        var stepPress = rangePress / (PressRt.Length - 2);

        for (var i = 0; i < PressRt.Length; i++)
            PressRt[i] = (int)Math.Round(minPress + stepPress * i, MidpointRounding.AwayFromZero);

        stepPress = Math.Round(rangePress / (PressRt32.Length - 2),1,MidpointRounding.AwayFromZero);
        
        for (var i = 0; i < PressRt32.Length; i++)
            PressRt32[i] = (int)Math.Round(minPress + stepPress * i, MidpointRounding.AwayFromZero);
    }

    private void FillTWatRT()
    {
        var min = -40;
        var max = 150;
        var count = TwatRt.Length;

        var step = (max - min)/(count - 1);
            
        for (var i = 0; i < count; i++)
        {
            TwatRt[i] = min;
            min += step;
        }
    }

    private void FillPoints()
    {
        var index = 0;
        
        for (var i = 0; i < 16; i++)
        for (var j = 0; j < 16; j++)
        {
            Rpm16_16RtPoints[index] = j;// rpmRt32[j];
            ThrRtPoints[index] = i;// thrRt[i];
            GbcRtPoints[index] = i;// gbcRt[i];
            PressRtPoints[index++]= i;
        }

        index = 0;
        for (var i = 0; i < 16; i++)
        for (var j = 0; j < 32; j++)
        {
            Rpm32_16RtPoints[index++] = j;// rpmRt32[j];
        }

        index = 0;
        for (var i = 0; i < 32; i++)
        for (var j = 0; j < 32; j++)
        {
            PressRt32Points[index] = i;// press32Rt32[i];
            Rpm32_32RtPoints[index++] = j;// rpm32Rt32[j];
        }
    }

    private void FillKGbc()
    {
        var colCount = J7esFlags.IsKgbc32_16 ? 32 : 16;
        Kgbc = new TableValues<byte, float>(FirmwareHelper.KGbcAddr, colCount, 16,
            source =>
                (float)
                Math.Round(source/128f, 2,
                    MidpointRounding.AwayFromZero),
            value =>
                (byte)
                Math.Round((value > 1.992f ? 1.992f : value)*128f,
                    MidpointRounding.AwayFromZero));

        InitTable(Kgbc, Buffer);
        Kgbc.PropertyChanged += KGBOnPropertyChanged;            
    }

    private void FillKGbcPress()
    {
        var colCount = J7esFlags.IsKgbcPress32_32 ? 32 : 16;
        var rowCount = J7esFlags.IsKgbcPress32_32 ? 32 : 16 ;

        Kgbc_press = new TableValues<byte, float>(FirmwareHelper.KGbcPressAddr, colCount, rowCount,
            source =>
                (float)
                Math.Round(source / 128f, 2,
                    MidpointRounding.AwayFromZero),
            value =>
                (byte)
                Math.Round((value > 1.992f ? 1.992f : value) * 128f,
                    MidpointRounding.AwayFromZero));

        InitTable(Kgbc_press, Buffer);
    }

    private void FillGbc()
    {
        Gbc = new TableValues<byte, short>(FirmwareHelper.GbcAddr, 16, 16,
            source =>
                (short)
                (Math.Round(source*8f/3f,
                    MidpointRounding.AwayFromZero)),
            value =>
                (byte)
                Math.Round(value*3f/8f,
                    MidpointRounding.AwayFromZero));

        InitTable(Gbc, Buffer);
        Gbc.PropertyChanged += GBCOnPropertyChanged;
    }

    private void InitTable(ITableValues table, byte[] source)
    {
        var index = table.Address;

        for (var i = 0; i < table.RowCount; i++)
        for (var j = 0; j < table.ColCount; j++)
        {
            table.SetRawValue(j, i, source[index++]);
        }

        table.FirstInit();
        table.FillValues();
    }

    private void FillGbcRT()
    {
        minGbc = Buffer[FirmwareHelper.MinGbcAddr]/6f;
        var stepK = Buffer[FirmwareHelper.StepGbcAddr];
        
        if (stepK == 0) stepK = 195;
        
        stepGbc = 341.13f/stepK;

        for (var i = 0; i < GbcRt.Length; i++)
            GbcRt[i] = (int) (minGbc + stepGbc*i*16);
    }

    private void FillThrRT()
    {
        System.Buffer.BlockCopy(Buffer, FirmwareHelper.ThrSamplingAddr, _thrSampling, 0, _thrSampling.Length);

        var index = 0;
        var value = index * 16;
        for (var i = 0; i < _thrSampling.Length; i++)
        {
            var delta = _thrSampling[i] - value;

            if (index == 0)
            {
                if (delta <= 0) continue;
                ThrRt[index] = Math.Max(i - 1, 0);
            }
            else
            {
                if (delta < 0) continue;
                ThrRt[index] = i;
            }

            index++;
            if (index == 16) break;
            value = index * 16;
        }
    }        

    private void CalcCurrentRT(DiagData diagData)
    {                                                            
        oldRpmThrRtIndex = RpmThrRtIndex;

        var j7esDiag = diagData as J7esDiagData;

        if (j7esDiag != null && j7esDiag.RPM_RT != 0)
        {
            ThrRtIndex = j7esDiag.THR_RT_16;
            GbcRtIndex = j7esDiag.GBC_RT_16;
            RpmRtIndex = j7esDiag.RPM_RT_16;
            RpmRt32Index = DataHelper.Rl(DataHelper.Swap((byte) (j7esDiag.RPM_RT + 4))) & 0x1F;
            TwatRtIndex = j7esDiag.TWAT_RT;
            RpmThrRtIndex = j7esDiag.RPM_THR_RT;
            RpmPressRtIndex = (((byte) (j7esDiag.PRESS_RT + 8)) & 0xF0) + RpmRtIndex;                
        }
        else
        {
            ThrRtIndex = DataHelper.Swap((byte)(_thrSampling[Math.Min(diagData.TRT, _thrSampling.Length - 1)] + 8)) & 0xF;
            GbcRtIndex = DataHelper.NearCell(GbcRt, (int)Math.Round(diagData.GBC, MidpointRounding.AwayFromZero));
            RpmRtIndex = DataHelper.NearCell(_rpmRt16, diagData.RPM);
            RpmRt32Index = DataHelper.NearCell(_rpmRt32, diagData.RPM);
            TwatRtIndex = DataHelper.NearCell(TwatRt, diagData.TWAT);
            RpmThrRtIndex = 16 * ThrRtIndex + RpmRtIndex;
            Rpm32PressRtIndex = (((byte)(j7esDiag.PRESS_RT + 8)) & 0xF0) + RpmRt32Index;
        }

        Rpm32ThrRtIndex = 32 * ThrRtIndex + RpmRt32Index;
            
        RpmGbcRtIndex = onlineManager.OltProtocol.Version == OltProtocolVersion.OltDiagV1
            ? diagData.RPM_GBC_RT
            : 16*GbcRtIndex + RpmRtIndex;
    }        

    private void PrepareFuelCutoff()
    {            
        PrepareFuelCutoff(onlineManager.settings.DisabledTHRZeroFuelCutoff);
    }

    private void PrepareFuelCutoff(bool value)
    {
        var options = Buffer[0x6051];
        //bit06 - признак постоянного включения топлива
        if (DataHelper.IsBitSet(options, 6) == value) return;
        DataHelper.BitSet(ref options, 6, value);
        onlineManager.OltProtocol.WriteRam(0x6051, new[] { options });
        Buffer[0x6051] = options;
    }

    public void Add(DiagData diagData)
    {
        if (!IsOpened) return; 
        CalcCurrentRT(diagData);

        if (!onlineManager.EnabledOnlineCorrection) return;

        PrepareFuelCutoff();
        if (!TestStability(diagData)) return;

        var cellIndex = J7esFlags.IsKgbc32_16 ? Rpm32ThrRtIndex : RpmThrRtIndex;
        var cell = (isVE ? Kgbc_press : Kgbc).Cell(cellIndex);
        if (!cell.StopStudy)
        {
            FuelCorrection(cellIndex, diagData);
        }

        if (onlineManager.settings.EnabledGBCCorrection && (!onlineManager.settings.TestAfrBeforeGBCCorrection || cell.StopStudy))
            AirCorrection(diagData);
    }

    private bool TestStability(DiagData diagData)
    {
        var stability = oldRpmThrRtIndex == RpmThrRtIndex 
                        && Math.Abs(diagData.DGTC_LEAN) < float.Epsilon 
                        && Math.Abs(diagData.DGTC_RICH) < float.Epsilon;

        if (!stability)
        {
            if (stabylityCount > 0)
                stabylityCount = 0;
            
            return false;
        }

        stabylityCount++;
        return stabylityCount >= onlineManager.settings.FuelRtStability;
    }

    private void AirCorrection(DiagData diagData)
    {
        if (!TestAirCorrectionAvailable(diagData)) return;
        GBCCorrection(diagData);
        stabylityCount = -2;
    }

    private bool TestAirCorrectionAvailable(DiagData diagData) 
        => diagData.COEFF is > 0.98f and < 1.02f;

    private void FuelCorrection(int cellIndex, DiagData diagData)
    {
        if (!TestFuelCorrectionAvailable(diagData)) return;
        
        KGBCCorrection(cellIndex, diagData);
        stabylityCount = -2;
    }

    private bool TestFuelCorrectionAvailable(DiagData diagData)
    {
        //var errorFound = diagData.IsErrorFound;
        var thrThreshold = diagData.TRT >= onlineManager.settings.THRThresholdLineFuelCorrection;            
        var lambdaAvailable = onlineManager.LambdaAdapter.Connected && onlineManager.LambdaAdapter.Available && onlineManager.LambdaAdapter.State == LambdaState.LambdaValue;

        return /*errorFound &&*/ thrThreshold && lambdaAvailable;
    }

    private void KGBCCorrection(int cellIndex, DiagData diagData)
    {
        var cell = (isVE ? Kgbc_press: Kgbc).Cell(cellIndex);
        //var k1 = kgbc.GetValue(cellIndex);
        var k1 = cell.StudyValue;
        var alf1 = diagData.ALF;
        var alf2 = diagData.LC1_ALF;
        var k2 = alf2*k1/alf1;

        var e = k2 - k1;
            
        cell.Error = e;
        cell.Tag = Math.Abs(e) < 0.02 ? Math.Max(5, cell.Tag + 1) : Math.Min(0, cell.Tag - 1);

        cell.StopStudy = cell.Tag > 4;
        if (cell.StopStudy) return;
            
        var e1 = cell.E_1;
        var e2 = cell.E_2;

        var kp = onlineManager.settings.DisableFuelCorrectionProportional
            ? 0f
            : onlineManager.settings.FuelCorrectionProportional/100f;
        var kd = onlineManager.settings.FuelCorrectionDifferential / 100f;
        var ki = onlineManager.settings.FuelCorrectionIntegral / 100f;

        var pV = kp*(e - e1);
        var iV = ki*e;
        var dV = kd*(e - 2*e1 + e2);

        var kNew = k1 + pV + iV + dV;
        cell.StudyValue = kNew;

        var kNewLim = Math.Max(kmin, Math.Min(kmax, kNew));
            
        (isVE? Kgbc_press: Kgbc).SetValue(cellIndex, kNewLim);            
        WriteKGBCValue(cellIndex);

        onlineManager.OltProtocol.OperationLog.AppendLine(string.Format("kgbc: {0} {1} {2} {3} {4} {5}|{6}|{7}",
            cellIndex, k1.ToString("0.##"),
            k2.ToString("0.##"), e.ToString("0.##"),
            kNew.ToString("0.##"), pV.ToString("0.##"),
            iV.ToString("0.##"), dV.ToString("0.##")));            
    }

    public void WriteKGBCValue(int index)
    {
        var source = isVE ? Kgbc_press[index] : Kgbc[index];
        var e = new AutoCorrectionEventArgs((isVE ? Kgbc_press:Kgbc).Address, index, source);
        OnAutoCorrection(e);
        
        if (e.Cancel) return;

        var address = (isVE ? Kgbc_press: Kgbc).CalcAddress(index);
        WriteRam(address, address, source);
    }

    private void OnAutoCorrection(AutoCorrectionEventArgs e)
    {
        var handler = AutoCorrection;
        handler?.Invoke(this, e);
    }

    private void GBCCorrection(DiagData diagData)
    {
        var cell = Gbc.Cell(RpmThrRtIndex);
        var realGbc = diagData.GBC;
        var gbc = Gbc.GetValue(RpmThrRtIndex);            
        var e = realGbc - gbc;

        cell.Error = (short) e;
        var e1 = cell.E_1;
        var e2 = cell.E_2;

        var kp = onlineManager.settings.DisableFuelCorrectionProportional
            ? 0f
            : onlineManager.settings.FuelCorrectionProportional / 100f;
        var kd = onlineManager.settings.FuelCorrectionDifferential / 100f;
        var ki = onlineManager.settings.FuelCorrectionIntegral / 100f;

        var pV = kp * (e - e1);
        var iV = ki * e;
        var dV = kd * (e - 2 * e1 + e2);

        var newGbc = gbc + pV + iV + dV;
        Gbc.SetValue(RpmThrRtIndex,  (short) newGbc);
        WriteGBCValue(RpmThrRtIndex);
        onlineManager.OltProtocol.OperationLog.AppendLine(string.Format("gbc: {0} {1} {2} {3}|{4}|{5} {6} {7}|{8}|{9}",
            RpmThrRtIndex, realGbc.ToString("0.##"),
            gbc.ToString("0.##"), e.ToString("0.##"), e1.ToString("0.##"), e2.ToString("0.##"),
            newGbc.ToString("0.##"), pV.ToString("0.##"),
            iV.ToString("0.##"), dV.ToString("0.##")));            
    }

    public void WriteGBCValue(int index)
    {           
        var source = Gbc[index];
        var e = new AutoCorrectionEventArgs(Gbc.Address, index, source);
        OnAutoCorrection(e);
        
        if (e.Cancel) return;

        var address = Gbc.CalcAddress(index);
        WriteRam(address, address, source);
    }

    public void WriteRam(int ramAddres, int bufferAddress, byte source, bool async = false)
    {
        onlineManager.OltProtocol.WriteRam(ramAddres, [source], async: async);
        Buffer[bufferAddress] = source;
        SaveToFile();
    }

    public void WriteRam(int ramAddres, int bufferAddress, byte[] source, IProgress progress = null, bool async = false)
    {
        if (onlineManager.OltProtocol.IsSupportOnlineCorrection)
            onlineManager.OltProtocol.WriteRam(ramAddres, source, progress, async);

        System.Buffer.BlockCopy(source, 0, Buffer, bufferAddress, source.Length);
        SaveToFile();
    }

    public float[] GetAxis(Entry2D entry2D) 
        => GetAxis(entry2D.xTable, entry2D.xStart, entry2D.xEnd, entry2D.xPoints);

    public float[] GetAxisX(Entry3D entry3D) 
        => GetAxis(entry3D.xTable, entry3D.xStart, entry3D.xEnd, entry3D.xPoints);

    public float[] GetAxisY(Entry3D entry3D) 
        => GetAxis(entry3D.zTable, entry3D.zStart, entry3D.zEnd, entry3D.zPoints);

    public float[] GetAxis(string name, double start, double end, int count)
    {
        switch (name)
        {
            case "!001-613C-40":
            case "!002-613C-40":
                return (from item in _rpmRt32 select (float)item).ToArray();

            case "!004-6064-6066":
                return (from item in GbcRt select (float)item).ToArray();

            case "!006-7208":
                return (from item in ThrRt select (float)item).ToArray();

            case "!004-5EF2-5EF4":
                return (from item in PressRt select (float)item).ToArray();
                    
            case "":
                var step = (end - start) / (count - 1);
                return
                    Enumerable.Range(0, count).Select(
                        index =>
                            (float)
                            Math.Round(start + step*index, 2, MidpointRounding.AwayFromZero)).ToArray();

            default:
                return null;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        var handler = PropertyChanged;
        handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}