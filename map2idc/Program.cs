using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace map2idc;

class Program
{
    [DllImport("kernel32.dll")]
    private static extern uint GetPrivateProfileString(
        string lpAppName,
        string lpKeyName,
        string lpDefault,
        StringBuilder lpReturnedString,
        int nSize,
        string lpFileName);

    private const int BufferSize = 100;
    private static string out_file;
    private static string in_file;
    private static StreamWriter file;
    private static StringBuilder buffer;

    static void Main(string[] args)
    {
        args = Environment.GetCommandLineArgs();
        Console.WriteLine("CTP map to ida script translator. cYbeR_mAD 2010 (c)");
        
        if (args.Length != 2)
        {
            Console.WriteLine("using: map2idc ctp_map.ini");
            return;
        }
        
        buffer = new StringBuilder(BufferSize);
        var dir = Path.GetDirectoryName(args[0]);
        
        if (string.IsNullOrEmpty(dir))
            dir = Environment.CurrentDirectory;

        var path = dir + "\\";
        in_file = path + args[1];
        out_file = in_file + ".adr";
        
        if (!File.Exists(in_file))
        {
            Console.WriteLine($"file {out_file} not found");
            return;
        }
                    
        var start = 0x10;
        file = new StreamWriter(out_file, false, Encoding.GetEncoding(866));
        
        var type = ReadIniParameter(buffer, start, "Type");

        while (!string.IsNullOrEmpty(type))
        {
            PrerapeSection(buffer, start, type);
            start += 0x10;
            type = ReadIniParameter(buffer, start, "Type");
            Console.Write('.');
        }

        file.Flush();
        file.Close();

        Console.WriteLine();
        Console.WriteLine("translate successful complete");
    }

    private static void PrerapeSection(StringBuilder buffer, int start, string type)
    {
        string addres;
        string name;
        string nameX;
        string nameF;
        switch (type)
        {
            case "TEXT":
            case "BIT":
                break;

            case "1D":
                addres = ReadIniParameter(buffer, start, "Addr");
                name = ReadIniParameter(buffer, start, "Name");
                nameX = ReadIniParameter(buffer, start, "Unit");                    

                WriteSection(addres, $"{name}, {nameX}");
                break;

            case "2D":
                addres = ReadIniParameter(buffer, start, "Addr");
                name = ReadIniParameter(buffer, start, "Name");
                nameX = ReadIniParameter(buffer, start, "NameX");
                nameF = ReadIniParameter(buffer, start, "NameT");

                WriteSection(addres, $"{name} X - {nameX} F - {nameF}");
                break;

            case "3D":
                addres = ReadIniParameter(buffer, start, "Addr");
                name = ReadIniParameter(buffer, start, "Name");
                nameX = ReadIniParameter(buffer, start, "NameX");
                nameF = ReadIniParameter(buffer, start, "NameT");
                var nameY = ReadIniParameter(buffer, start, "NameY");

                WriteSection(addres, $"{name} X - {nameX} Y - {nameY} F - {nameF}");
                break;
        }
    }

    private static void WriteSection(string addres, string comment)
    {
        var hex_addres = Convert.ToInt32(addres, 16);
        file.WriteLine($"{hex_addres.ToString("X4")}{comment}");
    }

    private static string ReadIniParameter(StringBuilder buffer, int start, string name)
    {
        var count = GetPrivateProfileString(
            start.ToString("X4"),
            name,
            string.Empty,
            buffer,
            buffer.Capacity,
            in_file);
        
        return count > 0 ? buffer.ToString() : string.Empty;
    }        
}
