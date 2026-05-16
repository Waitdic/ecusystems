using System;
using System.IO;
using System.Windows.Forms;
using EcuCommunication.Protocols;
using Helper.ProgressDialog;
using OpenOLT.Firmware;
using OpenOltTypes;
using WidebandLambdaCommunication;

namespace OpenOLT;

public class OnlineManager: IOnlineManager
{
    internal readonly Settings settings;
    internal readonly DataLogger dataLogger;
    internal readonly DiagDataKeeper dataKeeper;

    public IFirmwareManager FirmwareManager => _firmwareManager;

    public OltProtocol OltProtocol { get; }
    public LambdaAdapter LambdaAdapter { get; }
    
    private readonly FirmwareManager _firmwareManager;

    public OnlineManager(OltProtocol oltProtocol, LambdaAdapter lambdaAdapter)
    {
        settings = new Settings();            
        settings.LoadFromFile();
        dataLogger = new DataLogger();
        dataKeeper = new DiagDataKeeper();
        dataKeeper.LoadSettings(Settings.settingsKeeper);
        _firmwareManager = new FirmwareManager(this);            
        OltProtocol = oltProtocol;
        LambdaAdapter = lambdaAdapter;

        if (settings.AutoLoadLastFirmware)            
            _firmwareManager.Open(settings.LastFirmwarePath);            
            
        oltProtocol.OnDiagUpdate += oltProtocol_OnDiagUpdate;
        oltProtocol.OnConnect += oltProtocol_OnConnect;
        oltProtocol.OnDisconnect += oltProtocol_OnDisconnect;
    }

    private void oltProtocol_OnDisconnect(object sender, EventArgs e)
    {
        dataLogger.Flush();
            
        var basePath = Application.StartupPath + @"\trace\";
        if (!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);

        var fileName = $"oplog_{DateTime.Now.ToString("yyyy-MM-dd")}_{DateTime.Now.ToString("HH-mm-ss")}.txt";
        var filePath = basePath + fileName;
        File.WriteAllText(filePath, OltProtocol.OperationLog.ToString());
    }

    public bool EnabledOnlineCorrection { get; set; }
    public bool EnabledRamOnlineCorrection { get; set; }

    private void oltProtocol_OnConnect(object sender, EventArgs e) 
        => dataKeeper.diagDataList.Clear();

    private void oltProtocol_OnDiagUpdate(object sender, EventArgs e)
    {
        var diagData = OltProtocol.GetDiagData();
        diagData.LC1_AFR = LambdaAdapter.AFR;
        diagData.LC1_ALF = LambdaAdapter.Lambda;

        dataKeeper.diagDataList.Add(diagData);
        dataLogger.WriteData(diagData);
        _firmwareManager.Add(diagData);
        if (OltProtocol.IsEcuErrorFound && settings.AutoClearErrors)
            OltProtocol.ClearErrors();
    }

    public void Close()
    {
        dataLogger.Close();
        _firmwareManager.Close();
    }

    public void InitFirmware(IWin32Window owner)
    {
        if (!OltProtocol.Connected || !OltProtocol.IsOnline) return;

        switch (settings.LoadFirmwareToEcuType)
        {
            case LoadFirmwareToEcuType.FullLoad:
                OltProtocol.WriteFirmware(owner, _firmwareManager.Buffer);
                break;

            case LoadFirmwareToEcuType.OnlyCorrectionTable:
                WriteCorrectionTable(owner);
                break;
        }
    }

    private void WriteCorrectionTable(IWin32Window owner)
    {
        using var progress = ProgressForm.ShowProgress(owner);
        progress.Message = "Загрузка калибровок";                
        OltProtocol.WriteRam(_firmwareManager.Kgbc.Address, _firmwareManager.Kgbc.GetRawBuffer());
        progress.IterationComplete(this, 50, 100);
        OltProtocol.WriteRam(_firmwareManager.Kgbc_press.Address, _firmwareManager.Kgbc_press.GetRawBuffer());
        progress.IterationComplete(this, 50, 100);
        OltProtocol.WriteRam(_firmwareManager.Gbc.Address, _firmwareManager.Gbc.GetRawBuffer());
        progress.IterationComplete(this, 100, 100);
        progress.Close();
    }
}