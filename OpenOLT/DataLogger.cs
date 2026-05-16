using System;
using System.IO;
using System.Windows.Forms;
using EcuCommunication.Protocols;

namespace OpenOLT;

class DataLogger: IDisposable
{
    private FileStream _file;
    private StreamWriter _writer;
    private byte _count;

    public bool Enabled { get; set; }

    public DataLogger()
    {        
        Enabled = true;
    }        

    public void WriteData(DiagData diagData)
    {
        if (!Enabled) return;

        if (_file == null)
        {
            InitLogFile();
            _writer.WriteLine(diagData.GetLogHeader());
        }

        _writer.WriteLine(diagData.GetDataRow());
        _count++;
        
        if (_count < 100) return;
        
        Flush();
    }

    public void Flush()
    {
        if (_count == 0 || _writer == null) return;
        _writer.Flush();
        _count = 0;
    }

    private void InitLogFile()
    {            
        var basePath = Application.StartupPath + @"\logs\";
        if (!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);

        var fileName = $"{DateTime.Now.ToString("yyyy-MM-dd")}_{DateTime.Now.ToString("HH-mm-ss")}.csv";
        var filePath = basePath + fileName;
        _file = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(_file);            
    }

    public void Close()
    {
        if (_file == null) return;
        _writer.Flush();
        _writer.Close();
        _writer = null;

        _file.Close();
        _file = null;
    }

    #region Implementation of IDisposable

    public void Dispose()
    {
        Close();
    }

    #endregion
}