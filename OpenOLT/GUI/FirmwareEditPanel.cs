using System;
using System.Windows.Forms;
using OpenOLT.Map;

namespace OpenOLT.GUI;

public partial class FirmwareEditPanel : UserControl
{
    private readonly FirmwareMap _map = new();

    public FirmwareEditPanel()
    {
        InitializeComponent();
    }
        
    public void Open()
    {
        _map.Open();

        firmwareMap.Nodes.Clear();            

        foreach (var table in _map.Tables)
        {
            var category = firmwareMap.Nodes[table.Category] 
                           ?? firmwareMap.Nodes.Add(table.Category, _map.Categories[table.Category]);
            
            var tableNode = category.Nodes.Add(table.Title);
            tableNode.Tag = table;
        }
    }

    private void firmwareMapOpen_Click(object sender, EventArgs e) => Open();
}