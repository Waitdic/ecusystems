using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SokolovSport.Dat;

class DatFile: IDisposable
{
    public Dictionary<string, CalibrItem> Calibrations { get; } = new();

    public CalibrItem[] logCalibrItems;
    public CalibrItem[] onlineCalibrItems;

    public event EventHandler<CalibrChangeEventArgs> CalibrChange;
    public string Path { get; private set; }
    
    private readonly FileStream _fileStream;
    private readonly StreamWriter _writer;        

    public DatFile(string path)
    {
        Path = path;
        _fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0x1000, FileOptions.SequentialScan);
        string source;
        using (var reader = new StreamReader(_fileStream, Encoding.GetEncoding(866)))
        {
            source = reader.ReadToEnd();
            reader.Close();
        }
        _fileStream.Close();

        _fileStream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite, 0x1000, FileOptions.SequentialScan);
        _writer = new StreamWriter(_fileStream, Encoding.GetEncoding(866), 0x1000);

        var partIndex = 0;
        CalibrItem calibr = null;
        var items = source.Split([Environment.NewLine], StringSplitOptions.None);

        foreach(var item in items)
        {
            if (partIndex == 0)
            {
                calibr = new CalibrItem(this);
                calibr.PropertyChanged += calibr_PropertyChanged;
            }

            calibr.LoadFromPartString(item);

            int stopPartIndex;
            var itemType = calibr.ItemInfo?.ItemType ?? ItemTypes.Unknown;

            stopPartIndex = itemType switch
            {
                ItemTypes.Var => 7,
                ItemTypes.Table or ItemTypes.Teach => 7 + calibr.ItemInfo.RowCount,
                ItemTypes.Const => 8,
                _ => int.MaxValue
            };

            if (partIndex == stopPartIndex)
            {                    
                partIndex = 0;
                Calibrations.Add(calibr.Name, calibr);
            }
            else
                partIndex++;
        }

        Prepare();
    }

    public void SaveToFile()
    {
        _fileStream.Seek(0, SeekOrigin.Begin);

        _writer.Write(ToString());
        _writer.Flush();
    }

    private void Prepare()
    {
        foreach (var calibration in Calibrations.Values)
        {
            if (Calibrations.TryGetValue(calibration.AxisX, out var calibration1))
            {
                calibration.AxisXCalibrItem = calibration1;
            }
            if (Calibrations.TryGetValue(calibration.AxisY, out var calibration2))
            {
                calibration.AxisYCalibrItem = calibration2;
            }
            if (Calibrations.TryGetValue(calibration.VisualCalibr1Name, out var calibration3))
            {
                calibration.VisualCalibr1 = calibration3;
            }
            if (Calibrations.TryGetValue(calibration.VisualCalibr2Name, out var calibration4))
            {
                calibration.VisualCalibr2 = calibration4;
            }
        }

        logCalibrItems =
            Calibrations.Values.Where(
                    item => item.ItemInfo.ItemType is ItemTypes.Var or ItemTypes.Table).
                ToArray();

        onlineCalibrItems = Calibrations.Values.Where(item => item.ItemInfo.ItemType == ItemTypes.Var).ToArray();
    }

    private void calibr_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        var calibr = (CalibrItem) sender;
        DoCalibrChange(new CalibrChangeEventArgs(calibr, e.PropertyName));
    }

    public override string ToString()
    {
        return this.SaveToString();
    }

    public void Dispose()
    {            
        _writer.Close();
        _fileStream.Close();
    }

    private void DoCalibrChange(CalibrChangeEventArgs e)
    {
        var cc = CalibrChange;
        cc?.Invoke(this, e);
    }
}

internal class CalibrChangeEventArgs(CalibrItem calibr, string propertyChange) : EventArgs
{
    public CalibrItem Calibr { get; private set; } = calibr;
    public string PropertyChange { get; private set; } = propertyChange;
}