using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace OpenOLT.Map;

class FirmwareMap
{
    private XDocument _map;

    public List<Table> Tables { get; }
    public Dictionary<string, string> Categories { get; }

    public FirmwareMap()
    {
        Tables = new List<Table>();
        Categories = new Dictionary<string, string>();
    }

    public void Open()
    {
        var initPath = Application.StartupPath;
        if (Directory.Exists(initPath + @"\maps\"))
            initPath += @"\maps\";

        var openDialog = new OpenFileDialog
        {
            InitialDirectory = initPath,
            Filter = "map files|*.xdf|all files|*.*"
        };

        if (openDialog.ShowDialog() != DialogResult.OK) return;

        var fileName = openDialog.FileName;
        if (!File.Exists(fileName)) return;
            
        using (var reader = new StreamReader(fileName, Encoding.GetEncoding(1251)))
        {
            _map = XDocument.Load(reader);                
            reader.Close();
        }

        PrepareMapFile();
    }

    private void PrepareMapFile()
    {
        var root = _map?.Element("XDFFORMAT");

        var header = root?.Element("XDFHEADER");
        if (header == null) return;

        Categories.Clear();
        foreach (var category in header.Elements("CATEGORY"))
        {
            Categories.Add(category.Attribute("index").Value, category.Attribute("name").Value);
        }

        Tables.Clear();
        foreach (var table in root.Elements("XDFTABLE"))
            Tables.Add(new Table(table));
    }        
}