using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SokolovSport.Dat;
using SokolovSport.EcuComm;
using SokolovSport.Options;

namespace SokolovSport.Logs;

class CalibrLogger
{
    private readonly DatFile _datFile;
    private readonly EcuCommunication _ecuCommunication;
    private StreamWriter _logFile;
    private uint _lineCount;

    public CalibrLogger(DatFile datFile, EcuCommunication ecuCommunication)
    {
        _datFile = datFile;
        _ecuCommunication = ecuCommunication;

        ecuCommunication.OnCalibrSync += EcuCommunicationOnOnCalibrSync;
        ecuCommunication.PropertyChanged += EcuCommunicationOnPropertyChanged;
    }

    private void EcuCommunicationOnOnCalibrSync(object sender, EventArgs e)
    {
        if (!OptionsHelper.Options.LogECU) return;

        if (_logFile == null)
            Init();

        var dataLine = string.Join(",", _datFile.logCalibrItems.Select(item => item.ValueStr));
        _logFile.WriteLine(dataLine);

        if (_lineCount % 100 == 0) _logFile.Flush();
    }

    private void EcuCommunicationOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case "IsStarted":
                if (!_ecuCommunication.IsStarted && OptionsHelper.Options.NewLogOnConnectECU)
                    Close();
                break;
        }
    }

    private void Init()
    {
        var startPath = Application.StartupPath;
        var datDir = Path.GetFileNameWithoutExtension(_datFile.Path);
        var fileName = $"{DateTime.Now.ToString("yyyy-MM-dd")}_{DateTime.Now.ToString("HH-mm-ss")}.csv";            
        var path = $"{startPath}\\logs\\{datDir}\\{fileName}";
        var pathDir = Path.GetDirectoryName(path);

        if (!Directory.Exists(pathDir))
            Directory.CreateDirectory(pathDir);

        var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 2048);
        _logFile = new StreamWriter(stream, Encoding.GetEncoding(1251), 2048);

        var header = string.Join(",", _datFile.Calibrations.Values.Select(item => item.Description));            
        _logFile.WriteLine(header);

        _lineCount = 0;
    }

    public void Close()
    {
        if (_logFile == null) return;

        _logFile.Flush();
        _logFile.Close();
        _logFile = null;            
    }
}