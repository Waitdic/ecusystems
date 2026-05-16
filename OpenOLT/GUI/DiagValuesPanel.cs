using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EcuCommunication.Protocols;
using OpenOLT.DataValueInfo;

namespace OpenOLT.GUI;

internal partial class DiagValuesPanel : UserControl
{
    private readonly List<ValueInfo> _openedValues;
    private readonly ValueInfo[] _supportedValues;

    public DiagValuesPanel()
    {
        InitializeComponent();

        _supportedValues =
        [
            new ValueInfo("Обороты, RPMx100", 0, 10200, "RPM", 0),
            new ValueInfo("Дроссель, %", 0, 100, "TRT", 5),
            new ValueInfo("УОЗ", -65, 65, "UOZ", 7),
            new ValueInfo("Коррекция УОЗ", -50, 50, "DUOZ", 8),
            new ValueInfo("ТОЖ", -40, 150, "TWAT", 10),
            new ValueInfo("Твоздуха", -40, 150, "TAIR", 15),
            new ValueInfo("ALF", 0, 2, "ALF", 20),
            new ValueInfo("LC1_ALF", 0, 2, "LC1_ALF", 25),
            new ValueInfo("К. коррекции", 0, 2, "COEFF", 30),
            new ValueInfo("Время впрыска", 0, 530, "INJ", 35),
            new ValueInfo("Загрузка форс, %", 0, 100, "FUSE", 40),
            new ValueInfo("Массовый расход", 0, 1000, "AIR", 45),
            new ValueInfo("Цикловый расход", 0, 1000, "GBC", 50),
            new ValueInfo("Скорость, км/ч", 0, 255, "SPD", 55),
            new ValueInfo("Давление, кПа", 0, 656, "Press", 60),
            new ValueInfo("Желаемое давление, кПа", 0, 656, "TARGET_BOOST", 64),
            new ValueInfo("Открытие соленоида, %", 0, 100, "WGDC", 65),
            new ValueInfo("Turbo dynamics, %", -50, 50, "TURBO_DYNAMICS", 66),
            new ValueInfo("Уставка РХХ, кг/час", 0, 52, "UGB_RXX", 70)
        ];            

        _openedValues = new List<ValueInfo>([
            _supportedValues[0],
            _supportedValues[1],
            _supportedValues[2],
            _supportedValues[4],                                                      
            _supportedValues[6],
            _supportedValues[7],
            _supportedValues[11],
            _supportedValues[14],
            _supportedValues[15]
        ]);
            
        LoadSettings();
        Prepare();
    }        

    private void Prepare()
    {
        valuesPanel.SuspendLayout();

        var values = _openedValues.OrderByDescending(item => item.Order).ToArray();

        for (var index = 0; index < values.Length; index++)
        {
            var value = values[index];
            if (!valuesPanel.Controls.ContainsKey(value.Name))
            {
                var ctrl = new DiagValueControl {Dock = DockStyle.Top};
                ctrl.Init(value);
                valuesPanel.Controls.Add(ctrl);                    
            }
                
            valuesPanel.Controls.SetChildIndex(valuesPanel.Controls[value.Name], index);
        }

        var removedControls =
            valuesPanel.Controls.Cast<Control>().Where(item => !_openedValues.Contains(item.Tag)).ToArray();
            
        foreach (var control in removedControls)
        {
            valuesPanel.Controls.Remove(control);
            control.Tag = null;
            control.Dispose();
        }

        valuesPanel.ResumeLayout(true);
    }

    private void addBtn_Click(object sender, EventArgs e)
    {
        DiagValuesSelectForm.ShowSelectionDialog(this, _openedValues, _supportedValues);
        SaveSettings();
        Prepare();
    }

    private void LoadSettings()
    {
        var selectedValues = Settings.settingsKeeper.GetValue("SelectedDiagValues", string.Empty);
        if (string.IsNullOrEmpty(selectedValues)) return;

        var values = selectedValues.Split(';');
        _openedValues.Clear();
        _openedValues.AddRange(_supportedValues.Where(item => values.Contains(item.Name)));
    }

    private void SaveSettings()
    {
        var selectedValues = string.Join(";", _openedValues.Select(item => item.Name));
        Settings.settingsKeeper.SetValue("SelectedDiagValues", selectedValues);
    }

    public void UpdateValues(DiagData diagData)
    {
        foreach (DiagValueControl control in valuesPanel.Controls)
            control.SetValue(diagData);
    }
}