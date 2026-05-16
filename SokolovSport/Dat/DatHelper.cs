using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SokolovSport.EcuComm;

namespace SokolovSport.Dat;

static class DatHelper
{
    public static bool TestFile(string path)
    {
        return File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReadOnly) != FileAttributes.ReadOnly;
    }

    extension(CalibrItem calibr)
    {
        private float Convert2Value(float source)
        {
            if (calibr.ItemInfo == null || calibr.ItemInfo.Div == 0) return 0;

            var value = source * calibr.ItemInfo.Mul / calibr.ItemInfo.Div + calibr.ItemInfo.Offset;
            return value;
        }

        public float Convert2Value(int source)
        {
            return calibr.Convert2Value((float)source);
        }

        public int Convert2Source(float value)
        {
            if (calibr.ItemInfo == null || calibr.ItemInfo.Mul == 0) return 0;

            var source =
                (int)Math.Round((value - calibr.ItemInfo.Offset) * calibr.ItemInfo.Div / calibr.ItemInfo.Mul, MidpointRounding.AwayFromZero);
            return source;
        }

        public float CalcValueByIndex(int index, int count)
        {
            var step = calibr.ItemInfo.Length / (float)(count - 1);
            var rawValue = index*step;
            var value = calibr.Convert2Value(rawValue);
            return value;
        }

        public int CalcIndexByValue(float value, int count)
        {
            var rawValue = calibr.Convert2Source(value);
            var step = calibr.ItemInfo.Length / (float)(count - 1);
            return (int) Math.Floor((rawValue - calibr.ItemInfo.Min)/step);
        }

        public Request[] CreateWriteRequest(bool fullWrite = false)
        {
            switch (calibr.ItemInfo.ItemType)
            {
                case ItemTypes.Table:
                case ItemTypes.Teach:
                {
                    if (fullWrite)
                    {
                        var count = calibr.Table.Count;
                        var requests = new List<Request>();

                        for (int i = 0; i < count; i++)
                        {
                            requests.AddRange(calibr.CreateRequest(RequestType.Write, calibr.GetTableCellAddress(i),
                                calibr.Table.Cell(i).Source));
                        }

                        return requests.ToArray();
                    }

                    var cellIndex = calibr.Table.LastEditCellIndex;
                    return calibr.CreateRequest(RequestType.Write, calibr.GetTableCellAddress(cellIndex),
                        calibr.Table.Cell(cellIndex).Source);
                }

                default:
                    return calibr.CreateRequest(RequestType.Write, calibr.ItemInfo.Address, calibr.RawValue);
            }
        }

        public Request[] CreateRequest(RequestType requestType, ushort address, int rawValue)
        {                      
            var readRequests = Array.Empty<Request>();

            byte value;
            switch (calibr.ItemInfo.SizeType)
            {
                case SizeTypes.Int:
                case SizeTypes.UInt:                    
                    readRequests = new Request[2];
                    for (var i = 0; i < 2; i++)
                    {
                        value = (byte)((rawValue >> 8*(1 - i)) & 0xFF);
                        readRequests[i] = new Request(requestType, address++, value);
                    }
                    break;

                case SizeTypes.Char:
                case SizeTypes.UChar:
                case SizeTypes.Bit:                    
                    readRequests = new Request[1];
                    value = (byte) (rawValue & 0xFF);
                    readRequests[0] = new Request(requestType, address, value);                    
                    break;
            }

            return readRequests;
        }

        public ushort GetTableCellAddress(int cellIndex)
        {
            var cellSize = 1;
            switch (calibr.ItemInfo.SizeType)
            {
                case SizeTypes.Int:
                case SizeTypes.UInt:
                    cellSize = 2;
                    break;

                case SizeTypes.Char:
                case SizeTypes.UChar:
                case SizeTypes.Bit:
                    cellSize = 1;
                    break;
            }

            return (ushort) (calibr.ItemInfo.Address + cellIndex*cellSize);
        }

        private string SaveTableToString()
        {
            var values = new string[calibr.Table.ColCount];
            var buffer = new StringBuilder(calibr.Table.RowCount);

            for (var i = 0; i < calibr.Table.RowCount; i++)
            {
                for (var j = 0; j < calibr.Table.ColCount; j++)
                    values[j] = calibr.Table.Cell(j, i).Source.ToString(CultureInfo.InvariantCulture);
                
                buffer.AppendLine(String.Join(" ", values));
            }

            return buffer.ToString();
        }

        private string SaveValueToString()
        {
            return calibr.ItemInfo.ItemType switch
            {
                ItemTypes.Const => calibr.RawValue.ToString(CultureInfo.InvariantCulture) + Environment.NewLine,
                ItemTypes.Table or ItemTypes.Teach => calibr.SaveTableToString(),
                _ => string.Empty
            };
        }

        private string SaveToString()
        {
            var buffer = new StringBuilder();

            buffer.AppendLine(calibr.Description);            
            buffer.AppendLine(calibr.Unit);            
            buffer.AppendLine(calibr.Name);            
            buffer.AppendLine(calibr.AxisX);            
            buffer.AppendLine(calibr.AxisY);
            buffer.AppendLine(calibr.VisualCalibr1Name);
            buffer.AppendLine(calibr.VisualCalibr2Name);
            buffer.AppendLine(calibr.ItemInfo.SaveToString());
            var value = calibr.SaveValueToString();
            
            if (!string.IsNullOrEmpty(value))
                buffer.Append(value);

            return buffer.ToString();
        }
    }

    public static string SaveToString(this DatFile datFile)
    {
        var buffer = new StringBuilder(datFile.Calibrations.Count);
        
        foreach (var calibr in datFile.Calibrations.Values)
            buffer.Append(SaveToString(calibr));

        return buffer.ToString();
    }        
}