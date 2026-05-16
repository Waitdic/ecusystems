using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MazdaVINEditor;

public partial class MainForm : Form
{
    private string _path;
    private byte[] _buffer;
    private string _source;

    public MainForm()
    {
        InitializeComponent();
    }

    private unsafe void button1_Click(object sender, EventArgs e)
    {
        if (openFileDialog1.ShowDialog(this) != DialogResult.OK) return;
        
        _path = openFileDialog1.FileName;
        
        if (!File.Exists(_path)) return;
        
        pathStatus.Text = _path;

        _buffer = File.ReadAllBytes(_path);
        fixed (byte* ptr = &_buffer[0])
            _source = new string((sbyte*)ptr, 0, _buffer.Length);

        textBoxId.Text = FindId();
        textBoxVin.Text = _source.Substring(0x7080, 17);
            

        saveButton.Enabled = true;
    }

    private string FindId()
    {
        var regex = new Regex(@"L([a-zA-Z_0-9]{10})", RegexOptions.Compiled);
        var res = regex.Match(_source);

        return res.Success ? res.Value : "ID не найден";
    }

    private void saveButton_Click(object sender, EventArgs e)
    {
        var vin = textBoxVin.Text;
        if (vin.Length != 17 && MessageBox.Show(this, "VIN должен содержать 17 символов. Заполнить недостающие символы пробелами и сохранить?", "Некорректный VIN", MessageBoxButtons.YesNo) != DialogResult.Yes) 
            return;

        vin = vin.PadRight(17, ' ');
        
        for (var i = 0; i < vin.Length; i++)
            _buffer[0x7080 + i] = (byte) vin[i];

        using var saveFileDialog = new SaveFileDialog();
        saveFileDialog.FileName = _path;
        
        if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
            return;
        
        var savePath = saveFileDialog.FileName;

        File.WriteAllBytes(savePath, _buffer);
    }

    private void toolStripStatusLabel1_Click(object sender, EventArgs e)
    {
        Process.Start(@"http://ecusystems.ru");
        toolStripStatusLabel1.LinkVisited = true;
    }

    private void aboutButton_Click(object sender, EventArgs e)
    {
        using var about = new AboutBox();
        about.ShowDialog(this);
    }
}