using System;
using System.Linq;
using System.Windows.Forms;
using OpenOLT.DataValueInfo;

namespace OpenOLT.GUI;

internal partial class OpenCharts : Form
{
    private DiagDataKeeper _dataKeeper;        

    public OpenCharts()
    {
        InitializeComponent();
    }

    public void Init(DiagDataKeeper dataKeeper)
    {
        _dataKeeper = dataKeeper;

        foreach (var valueInfo in dataKeeper.valueInfos)
            allAvailabeCharts.Items.Add(valueInfo.Title);

        foreach (var chartSet in dataKeeper.chartSets)
            chartSetsComboBox.Items.Add(chartSet.Name);
    }

    private void allAvailabeCharts_DoubleClick(object sender, EventArgs e)
    {
        var chartIndex = allAvailabeCharts.SelectedIndex;
        if (chartIndex >= _dataKeeper.valueInfos.Length) return;
        var chart = _dataKeeper.valueInfos[chartIndex];
        if (selectedChars.Items.Contains(chart.Title) && MessageBox.Show(this, "Данный график уже добавлен. Хотети добавить еще раз?", 
                "Внимание", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;
            

        selectedChars.Items.Add(chart.Title);

        var currentSet = GetCurrentSet();
        currentSet?.Items.Add(chart);
    }

    private void selectedChars_DoubleClick(object sender, EventArgs e)
    {
        var chartIndex = selectedChars.SelectedIndex;
        if (chartIndex < 0) return;
        
        selectedChars.Items.RemoveAt(chartIndex);            

        var currentSet = GetCurrentSet();
        currentSet?.Items.RemoveAt(chartIndex);
    }

    private void chartSetsComboBox_Leave(object sender, EventArgs e)
    {
        if (selectedChars.Items.Count == 0) return;

        var setName = chartSetsComboBox.Text;
        if (string.IsNullOrWhiteSpace(setName)) return;

        if (_dataKeeper.chartSets.Any(item => item.Name == setName)) return;

        var set = new ChartSet{Name = setName};

        foreach (string item in selectedChars.Items)
        {
            var itemInfo = item;
            set.Items.Add(_dataKeeper.valueInfos.First(info => info.Title == itemInfo));
        }

        _dataKeeper.chartSets.Add(set);
        chartSetsComboBox.Items.Add(set.Name);
    }

    private ChartSet GetCurrentSet()
    {
        if (selectedChars.Items.Count == 0) return null;

        var setName = chartSetsComboBox.Text;
        if (string.IsNullOrWhiteSpace(setName)) return null;

        var chartSet = _dataKeeper.chartSets.FirstOrDefault(item => item.Name == setName);
        if (chartSet != null) return chartSet;
        
        chartSet = new ChartSet { Name = setName };
        _dataKeeper.chartSets.Add(chartSet);
        chartSetsComboBox.Items.Add(chartSet.Name);

        return chartSet;
    }

    private void chartSetsComboBox_KeyDown(object sender, KeyEventArgs e) 
        => selectedChars.Items.Clear();

    private void delSet_Click(object sender, EventArgs e)
    {
        var index = chartSetsComboBox.SelectedIndex;
        if (index < 0 || index >= _dataKeeper.chartSets.Count) return;

        _dataKeeper.chartSets.RemoveAt(index);
        selectedChars.Items.Clear();
        chartSetsComboBox.Text = string.Empty;
    }

    private void chartSetsComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        var index = chartSetsComboBox.SelectedIndex;
        if (index < 0 || index >= _dataKeeper.chartSets.Count) return;

        var chartSet = _dataKeeper.chartSets[index];

        selectedChars.Items.Clear();
        
        foreach (var item in chartSet.Items)
            selectedChars.Items.Add(item.Title);
    }

    public ValueInfo[] GetSelectedCharts()
    {
        var res =
            selectedChars.Items.OfType<string>().Select(
                item => _dataKeeper.valueInfos.First(info => info.Title == item)).ToArray();

        return res;
    }

    private void btnOpen_Click(object sender, EventArgs e)
    {
        var res =
            selectedChars.Items.OfType<string>().Select(
                item => _dataKeeper.valueInfos.First(info => info.Title == item)).ToArray();
            
        // return res;
    }
}