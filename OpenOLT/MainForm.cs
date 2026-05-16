using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using EcuCommunication.Protocols;
using Helper;
using Helper.Hooks;
using OpenOLT.GUI;
using OpenOltTypes;
using WidebandLambdaCommunication;

using Action = Crad.Windows.Forms.Actions.Action;

namespace OpenOLT;

public partial class MainForm : Form, IApplicationHost
{
    private readonly SynchronizationContext _uiContext;        
    private readonly OltProtocol _oltProtocol;
    private bool _fullScreen;
    private FormWindowState _windowState;
    private readonly OnlineManager _onlineManager;
    private DiagValuesPanel _diagValuePanel;

    public MainForm()
    {
        Cursor.Current = Cursors.WaitCursor;

        InitializeComponent();
            
        _oltProtocol = new OltProtocol();
        _oltProtocol.PropertyChanged += oltProtocol_PropertyChanged;            

        _onlineManager = new OnlineManager(_oltProtocol, lambdaAdapter);
        _onlineManager.FirmwareManager.OnOpenFirmware += FirmwareManagerOnOpenFirmware;            
        _uiContext = SynchronizationContext.Current;
        diagGaugePanel.Prepare(_oltProtocol, lambdaAdapter);
        diagChartPanel.Prepare(_onlineManager);
        rtGridPanel.Prepare(_onlineManager);

        PrepareOpenedFirmware();

        versionStatusLabel.Text = $"version {Assembly.GetExecutingAssembly().GetName().Version}";          
        Cursor.Current = Cursors.Default;

        _oltProtocol.OnDiagUpdate += OltProtocolOnOnDiagUpdate;
    }

    private void FirmwareManagerOnOpenFirmware(object sender, EventArgs eventArgs) 
        => PrepareOpenedFirmware();

    private void OltProtocolOnOnDiagUpdate(object sender, EventArgs eventArgs)
    {
        var diagData = _oltProtocol.GetDiagData();

        _uiContext.Send(
            delegate
            {
                rtGridPanel.DiagDataUpdate(diagData);                        
            }, null);
    }

    private void UpdateErrorStatus()
    {
        if (_oltProtocol.IsEcuErrorFound)
        {
            errorStatus.BackColor = Color.Gold;
            errorStatus.Text = "Found ecu error";
        }
        else
        {
            errorStatus.BackColor = SystemColors.Control;
            errorStatus.Text = "Error not found";
        }
    }

    private void oltProtocol_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case "Connected":
                oltProtocolConnectChange();
                break;

            case "IsOnline":
                _uiContext.Post(
                    delegate
                    {
                        UpdateOnlineStatus();
                    }, null);
                break;

            case "IsEcuErrorFound":
                _uiContext.Post(
                    delegate
                    {
                        UpdateErrorStatus();
                    }, null);
                break;
        }
    }

    private void oltProtocolConnectChange()
    {
        if (_oltProtocol.Connected)
        {
            lambdaAdapter.StartCommunication();
            updateThread.RunWorkerAsync();
        }
        else
        {
            if (_onlineManager.settings.NewLogOnConnectECU)
                _onlineManager.dataLogger.Close();

            lambdaAdapter.StopCommunication();
            updateThread.CancelAsync();
            BackgroundWorkerHelper.Wait(updateThread);
        }

        _uiContext.Post(
            delegate
            {
                UpdateConnectStatus();
                UpdateEcuDiagValues();
            }, null);
    }             

    private void EcuConnect()
    {
        if (_oltProtocol.Connected || _oltProtocol.IsBusy) return;

        ApplySettings();

        Cursor.Current = Cursors.WaitCursor;

        try
        {
            for (var i = 0; i < 5; i++)
            {
                if (_oltProtocol.Start()) break;
                Thread.Sleep(200);
                Application.DoEvents();
            }
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }

        UpdateConnectStatus();
        UpdateEcuDiagValues();
        UpdateLambdaStatus();
        UpdateLambdaValue();

        if (!_oltProtocol.Connected)
            MessageBox.Show(this, "Соединение с ЭБУ выполнить не удалось", "StartCommunication",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            
    }

    private void EcuDisconnect()
    {
        if (!_oltProtocol.Connected || _oltProtocol.IsBusy) return;
        Cursor.Current = Cursors.WaitCursor;
        _oltProtocol.Stop();

        UpdateConnectStatus();
        UpdateOnlineStatus();
        UpdateEcuDiagValues();
        UpdateLambdaStatus();
        UpdateLambdaValue();
        Cursor.Current = Cursors.Default;
    }

    private void ApplySettings()
    {
        ApplySettingsReboot();
        ApplySettingsImmediately();
    }

    private void ApplySettingsReboot()
    {
        _oltProtocol.PortName = _onlineManager.settings.PortName;
        _oltProtocol.BaundRate = _onlineManager.settings.BaundRate;
        _oltProtocol.Version = _onlineManager.settings.OltProtocolVersion;
        _oltProtocol.CalcEcuSn = _onlineManager.settings.CalcEcuSn;
        _oltProtocol.EcuSn = _onlineManager.settings.EcuSn;

        lambdaAdapter.PortName = _onlineManager.settings.LambdaPortName;
        lambdaAdapter.BaundRate = _onlineManager.settings.LambdaBaundRate;
        lambdaAdapter.Protocol = _onlineManager.settings.LambdaProtocol;

        DiagData.FullTimeMode = _onlineManager.settings.LogFullTimeMode;
    }

    private void ApplySettingsImmediately()
    {
        _oltProtocol.ReadTimeout = _onlineManager.settings.ReadTimeout;
        _oltProtocol.ReadFreq = _onlineManager.settings.ReadFreqNew;
        _oltProtocol.WriteTimeout = _onlineManager.settings.WriteTimeout;
        _oltProtocol.TraceEnabled = _onlineManager.settings.TraceECU;            
        lambdaAdapter.TraceEnabled = _onlineManager.settings.TraceLambda;
        lambdaAdapter.ReadTimeout = _onlineManager.settings.ReadLambdaTimeout;
        _onlineManager.dataLogger.Enabled = _onlineManager.settings.LogECU;            
    }

    #region Action handlers
    private void settingsDialogAction_Execute(object sender, EventArgs e)
    {
        var old = KeyboardHook.Enabled;
        KeyboardHook.Enabled = false;

        try
        {
            if (!SettingsHelper.ShowSettingsDialog(this, _onlineManager.settings)) return;
            ApplySettingsImmediately();
            _onlineManager.settings.SaveToFile();
        }
        finally
        {
            KeyboardHook.Enabled = old;
        }
    }

    private void exitAction_Execute(object sender, EventArgs e) => Close();

    private void connectAction_Execute(object sender, EventArgs e) => EcuConnect();

    private void disconnectAction_Execute(object sender, EventArgs e) => EcuDisconnect();

    private void showGaugePanel_Execute(object sender, EventArgs e)
    {
        diagGaugePanel.Visible = !diagGaugePanel.Visible;
        UpdateEcuDiagValues();
    }

    private void showGaugePanel_Update(object sender, EventArgs e) 
        => showGaugePanelAction.Checked = diagGaugePanel.Visible;

    private void showChartPanelAction_Execute(object sender, EventArgs e)
    {
        HideAllDataContent();
        diagChartPanel.Visible = true;
    }

    private void showFirmwareEditPanelAction_Execute(object sender, EventArgs e)
    {
        HideAllDataContent();
        firmwareEditorPanel.Visible = true;
    }

    private void openFirmwareMap_Execute(object sender, EventArgs e) 
        => firmwareEditorPanel.Open();

    private void openFirmwareAction_Execute(object sender, EventArgs e)
    {
        var old = KeyboardHook.Enabled;
        KeyboardHook.Enabled = false;

        try
        {
            _onlineManager.FirmwareManager.OpenDialog(this);
        }
        finally
        {
            KeyboardHook.Enabled = old;
        }
    }

    private void showRtGridAction_Execute(object sender, EventArgs e)
    {
        HideAllDataContent();
        rtGridPanel.Visible = true;
    }

    private void enabledOnlineCorrectionAction_Execute(object sender, EventArgs e)
    {
        if (!_onlineManager.EnabledOnlineCorrection && _onlineManager.FirmwareManager.SWDigest != _oltProtocol.SWDigest)
        {
            if (MessageBox.Show(this, "Прошивка не соответствует загруженной в ЭБУ. Игнорировать предупреждение и начать обучение?",
                    "Ошибка", MessageBoxButtons.YesNo, MessageBoxIcon.Error) != DialogResult.Yes)
                return;
        }

        _onlineManager.EnabledOnlineCorrection = !_onlineManager.EnabledOnlineCorrection;
    }

    private void enabledOnlineCorrectionAction_Update(object sender, EventArgs e)
    {
        enabledOnlineCorrectionAction.Enabled = (_oltProtocol.IsSupportOnlineCorrection &&
                                                 _onlineManager.FirmwareManager.IsOpened && !_oltProtocol.InitProgress) ||
                                                _onlineManager.EnabledRamOnlineCorrection;
        enabledOnlineCorrectionAction.Checked = _onlineManager.EnabledOnlineCorrection;
    }

    private void showErrorsFormAction_Execute(object sender, EventArgs e) 
        => ErrorCodesForm.ShowErrors(this, _oltProtocol);

    private void showErrorsFormAction_Update(object sender, EventArgs e) 
        => showErrorsFormAction.Enabled = _oltProtocol.IsEcuErrorFound;
    
    #endregion        

    private void UpdateEcuDiagValues()
    {
        var diagData = _oltProtocol.GetDiagData();
        diagGaugePanel.UpdateValue();
        _diagValuePanel?.UpdateValues(diagData);
        
        UpdateWarnStatus(diagData);
    }

    private void UpdateWarnStatus(DiagData diagData)
    {
        var j7esdd = diagData as J7esDiagData;
        warnTwatStatus.Visible = diagData.TWAT > _onlineManager.settings.WarnTwat;
        warnTairStatus.Visible = diagData.TAIR > _onlineManager.settings.WarnTair;
        warnFuseStatus.Visible = diagData.FUSE > _onlineManager.settings.WarnFuse;
        warnUbatStatus.Visible = diagData.ADCUBAT > _onlineManager.settings.WarnUBatMax || diagData.ADCUBAT < _onlineManager.settings.WarnUBatMin;
        warnPressStatus.Visible = j7esdd != null &&
                                  (j7esdd.Press > _onlineManager.settings.WarnPressMax ||
                                   j7esdd.Press < _onlineManager.settings.WarnPressMin);
    }

    private void UpdateConnectStatus()
    {
        ecuConnectionStatus.Text = _oltProtocol.Connected ? "ECU connected" : "ECU disconnected";
        ecuConnectionStatus.BackColor = _oltProtocol.Connected ? Color.LawnGreen : Color.Gold;                       
    }

    private void UpdateOnlineStatus()
    {
        onlineStatusLabel.Text = _oltProtocol.IsOnline ? "online supported" : "online no supported";
        onlineStatusLabel.BackColor = _oltProtocol.IsOnline ? Color.LawnGreen : Color.Gold;
    }

    private void fullScreenAction_Execute(object sender, EventArgs e)
    {
        if (!_fullScreen)
            _windowState = WindowState;

        _fullScreen = !_fullScreen;

        mainMenuStrip.Visible = mainToolStrip.Visible = !_fullScreen;            
        FormBorderStyle = _fullScreen ? FormBorderStyle.None : FormBorderStyle.Sizable;
        WindowState = _fullScreen ? FormWindowState.Maximized : _windowState;
    }

    private void lambdaAdapter_OnConnect(object sender, EventArgs e)
    {
        _uiContext.Post(
            delegate
            {
                UpdateLambdaStatus();
                UpdateLambdaValue();
            }, null);
    }

    private void UpdateLambdaStatus()
    {
        lambdaStatus.Text = lambdaAdapter.Connected ? "Lambda connected" : "Lambda disconnected";
        lambdaStatus.BackColor = lambdaAdapter.Connected ? Color.LawnGreen : Color.Gold;
    }

    private void UpdateLambdaValue()
    {
        lambdaValue.BackColor = SystemColors.Control;

        if (!lambdaAdapter.Available)
        {
            lambdaValue.Text = "LC1 - нет данных";
            lambdaValue.BackColor = Color.Gold;
        }
        else
            switch (lambdaAdapter.State)
            {
                case LambdaState.LambdaValue:
                    lambdaValue.Text = string.Format("LC1 - [{0}], [{1}]",
                        lambdaAdapter.Lambda.ToString("0.###", CultureInfo.InvariantCulture),
                        lambdaAdapter.AFR.ToString("0.###", CultureInfo.InvariantCulture));
                    break;

                case LambdaState.O2Level:
                    lambdaValue.Text = $"[LC1 - O2 Level [{lambdaAdapter.O2Level}%]";
                    lambdaValue.BackColor = Color.Gold;
                    break;

                case LambdaState.ErrorCode:
                    lambdaValue.Text = $"LC1 - Error Code [{lambdaAdapter.O2Level}]";
                    lambdaValue.BackColor = Color.Red;
                    break;
            }            
    }

    private void updateThread_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
    {
        while (true)
        {
            if (updateThread.CancellationPending) break;

            if (_oltProtocol.Connected ) 
            {
                EcuConnect();
            }
            
            _uiContext.Send(
                delegate
                {
                    UpdateEcuDiagValues();
                    UpdateLambdaValue();
                }, null);

            if (updateThread.CancellationPending) break;
            Thread.Sleep(_onlineManager.settings.UpdateDiagValuesFreq);
        }
    }

    private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
    {
        diagGaugePanel.Prepare(null, null);
        EcuDisconnect();
        _onlineManager.Close();
        _onlineManager.settings.SaveToFile();
    }

    private void linkStatuslable_Click(object sender, EventArgs e)
    {
        Process.Start(@"http://ecusystems.ru");
        linkStatuslable.LinkVisited = true;
    }

    private void PrepareOpenedFirmware()
    {
        if (!_onlineManager.FirmwareManager.IsOpened) return;
        firmwareStatusLabel.Text = _onlineManager.FirmwareManager.Name;
        firmwareStatusLabel.ToolTipText = _onlineManager.FirmwareManager.SWDigest.ToString("X8");
        dadModeStatusLabel.Visible = _onlineManager.FirmwareManager.J7esFlags.IsDadMode;            
        rtGridPanel.InitData();
        rtGridPanel.LoadData();
        _onlineManager.InitFirmware(this);            
    }

    private void showDiagValuesPanel_Execute(object sender, EventArgs e)
    {
        SuspendLayout();

        if (_diagValuePanel == null)
        {
            _diagValuePanel = new DiagValuesPanel {Dock = DockStyle.Left};
            Controls.Add(_diagValuePanel);
            Controls.SetChildIndex(_diagValuePanel, 1);
        }
        else
        {
            Controls.Remove(_diagValuePanel);
            _diagValuePanel.Dispose();
            _diagValuePanel = null;
        }
        ResumeLayout(true);
    }

    private void HideAllDataContent()
    {
        foreach (Control control in mainPanel.Controls)
            control.Visible = false;
    }

    private void showDiagValuesPanel_Update(object sender, EventArgs e) 
        => showDiagValuesPanel.Checked = _diagValuePanel != null;

    public void AddContent(Control control, string text, Image image, Keys shortcut, bool left = false)
    {
        control.Dock = left ? DockStyle.Left : DockStyle.Fill;
        control.Visible = false;

        if (left)
        {
            Controls.Add(control);
            Controls.SetChildIndex(control, 1);
        }
        else
            mainPanel.Controls.Add(control);

        var button = new ToolStripButton {DisplayStyle = ToolStripItemDisplayStyle.Image};
        var menuItem = new ToolStripMenuItem();
        var action = new Action {Text = text, ToolTipText = text, Image = image, ShortcutKeys = shortcut};
        action.Execute += (_, _) =>
        {
            if (left)
                control.Visible = !control.Visible;                        
            else
            {
                HideAllDataContent();
                control.Visible = true;
            }
        };
        
        action.Update += (sender, _) =>
        {
            var act = (Action) sender;
            act.Checked = control.Visible;
            act.Enabled = control.Enabled;
        };
        
        actionList.Actions.Add(action);
        actionList.SetAction(button, action);
        actionList.SetAction(menuItem, action);
        mainToolStrip.Items.Insert(mainToolStrip.Items.IndexOf(pluginSeparator) + 1, button);
        pagesMenu.DropDownItems.Add(menuItem);
    }

    public IOnlineManager GetOnlineManager() => _onlineManager;

    private void showRtGridAction_Update(object sender, EventArgs e) 
        => showRtGridAction.Checked = rtGridPanel.Visible;

    private void showChartPanelAction_Update(object sender, EventArgs e) 
        => showChartPanelAction.Checked = diagChartPanel.Visible;
}