using System;
using System.Collections.Generic;
using System.IO;
using CtpMaps.DataTypes;

namespace CtpMaps;

public unsafe class CtpMap
{
    public readonly List<MapEntry> entries = [];
    private byte[] _rawBuffer;
    private MapHeader _mapHeader;
    
    public string Path { get; private set; }

    public void LoadFromFile(string path)
    {
        if (!File.Exists(path)) return;

        Path = path;
        entries.Clear();

        _rawBuffer = File.ReadAllBytes(path);

        //43544D  - CTM
        if (_rawBuffer[0] != 0x43 || _rawBuffer[1] != 0x54 || _rawBuffer[2] != 0x4D)
            throw new Exception("Wrong file");

        fixed (byte* ptr = _rawBuffer)
        {
            var stopPtr = (int)ptr + _rawBuffer.Length;

            _mapHeader = *((MapHeader*) ptr);
            var entryHeaderPtr = ptr + sizeof (MapHeader);

            if (_mapHeader.Version_hi == 7)
            {
                entryHeaderPtr += 35;
            }

            var olt = _mapHeader.Version_hi == 1;

            while ((int)entryHeaderPtr < stopPtr)
            {
                var entryHeader = new MapEntry(ref entryHeaderPtr, olt);
                entries.Add(entryHeader);                                       
            }
        }
    }

    public void SaveToFile(string path, bool olt = false)
    {
        if (olt)
        {
            var oltMapHeader = new MapHeader();
            *oltMapHeader.Ctm = 0x43;
            *(oltMapHeader.Ctm + 1) = 0x54;
            *(oltMapHeader.Ctm + 2) = 0x4D;
            oltMapHeader.Version_hi = 1;
            oltMapHeader.Version_lo = 0;

            MapFactory.SaveToFile(path, oltMapHeader, entries, true);
        }
        else
        {
            MapFactory.SaveToFile(path, _mapHeader, entries);
        }
    }

    public void SaveToFile(string path, IEnumerable<MapEntry> entries, bool olt = false)
    {
        MapFactory.SaveToFile(path, _mapHeader, entries, olt);
    }
    
    public string CheckCompareId()
    {
        var rnd = new Random((int) DateTime.Now.Ticks);
        var message = string.Empty;

        foreach (var entry in entries)
        {
            switch (entry.Type)
            {
                case 5:
                    if (entry.IdentEntry.Comp_id is 0 or 0xFFFFFFFF)
                    {
                        entry.IdentEntry.Comp_id = (uint) rnd.Next(int.MinValue, int.MaxValue);
                        message += $"CompareID: {entry.IdentEntry.Comp_id}; Address: {entry.IdentEntry.Addr[0].ToString("X4")}\r\n ";
                    }
                    break;

                case 4:
                    if (entry.FlagsEntry.Comp_id is 0 or 0xFFFFFFFF)
                    {
                        entry.FlagsEntry.Comp_id = (uint) rnd.Next(int.MinValue, int.MaxValue);
                        message += $"CompareID: {entry.FlagsEntry.Comp_id}; Address: {entry.FlagsEntry.Addr[0].ToString("X4")}\r\n";
                    }
                    break;

                case 3:
                    if (entry.Entry1D.Comp_id is 0 or 0xFFFFFFFF)
                    {
                        entry.Entry1D.Comp_id = (uint) rnd.Next(int.MinValue, int.MaxValue);
                        message += $"CompareID: {entry.Entry1D.Comp_id}; Address: {entry.Entry1D.Addr.ToString("X4")}\r\n";
                    }
                    break;

                case 1:
                    if (entry.Entry2D.Comp_id is 0 or 0xFFFFFFFF)
                    {
                        entry.Entry2D.Comp_id = (uint) rnd.Next(int.MinValue, int.MaxValue);
                        message += $"CompareID: {entry.Entry2D.Comp_id}; Address: {entry.Entry2D.Addr.ToString("X4")}\r\n";
                    }
                    break;

                case 2:
                    if (entry.Entry3D.Comp_id is 0 or 0xFFFFFFFF)
                    {
                        entry.Entry3D.Comp_id = (uint) rnd.Next(int.MinValue, int.MaxValue);
                        message += $"CompareID: {entry.Entry3D.Comp_id}; Address: {entry.Entry3D.Addr.ToString("X4")}\r\n";
                    }
                    break;

            }
        }

        return message;
    }
}
