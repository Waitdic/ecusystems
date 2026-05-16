using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using CalibrTable;
using EcuCommunication.Protocols;
using Gigasoft.ProEssentials.Enums;
using Helper;
using Helper.Hooks;

namespace OpenOLT.GUI;

internal partial class RTGridPanel : UserControl
{
    private OnlineManager _onlineManager;
    private readonly PaletteFast _gbcPalette;
    private readonly PaletteFast _kgbcPalette;
    private readonly PaletteFast _kgbcStudyPalette;
    private readonly PaletteFast _gbcStudyPalette;
    private readonly int _backColor;
    private bool _followRt;
    private ITableValues _table;

    public RTGridPanel()
    {
        InitializeComponent();
        _gbcPalette = new PaletteFast(new Palette());
        _kgbcPalette = new PaletteFast(new Palette(), 100);
        _kgbcStudyPalette = new PaletteFast(new Palette(), 100);
        _gbcStudyPalette = new PaletteFast(new Palette(), 10);

        PrepareChart(16);
        SetStyle(ControlStyles.ResizeRedraw, true);

        _backColor = SystemColors.Window.ToArgb();

        _kgbcPalette.Palette.AddElementsRange(PaletteHelper.GetSymmetric(0f, 2f));
        _kgbcPalette.FillColors();

        _kgbcStudyPalette.Palette.AddElementsRange(PaletteHelper.GetLinear(0f, 1f));
        _kgbcStudyPalette.FillColors();

        short gbcMax = 600, gbcMin = 0;
        //onlineManager.FirmwareManager.Gbc.GetMinMax(out gbcMin, out gbcMax);            
        _gbcPalette.Palette.AddElementsRange(PaletteHelper.GetLinear(gbcMin, gbcMax));
        _gbcPalette.FillColors();

        _gbcStudyPalette.Palette.AddElementsRange(PaletteHelper.GetLinear(0f, 50f));
        _gbcStudyPalette.FillColors();

        KeyboardHook.Instance.KeyDown += KeyboardHookOnKeyDown;
    }

    private void KeyboardHookOnKeyDown(object sender, KeyboardHookEventArgs e)
    {
        if (!Visible || Form.ActiveForm != FindForm()) return;
        
        if (!_followRt)
        {
            var colCount = rtGrid.ColumnCount;
            var rowCount = rtGrid.RowCount;
            var cellCount = colCount * rowCount;

            var tableIndex = rtGrid.CurrentCellAddress.Y * colCount + rtGrid.CurrentCellAddress.X;
            switch (e.Key)
            {
                case Keys.D:
                    tableIndex = (tableIndex + 1) % cellCount;
                    SetCurrentCell(tableIndex);
                    break;

                case Keys.A:
                    tableIndex = (tableIndex - 1 + cellCount) % cellCount;
                    SetCurrentCell(tableIndex);
                    break;

                case Keys.W:
                    tableIndex = (tableIndex - colCount + cellCount) % cellCount;
                    SetCurrentCell(tableIndex);
                    break;

                case Keys.S:
                    tableIndex = (tableIndex + colCount) % cellCount;
                    SetCurrentCell(tableIndex);
                    break;
            }
        }

        if (!_onlineManager.FirmwareManager.IsOpened) return;
        
        switch (e.Key)
        {
            //case (Keys)(189):
            //case Keys.Subtract:
            case Keys.K:
                ChangeTableValue((ModifierKeys & Keys.Shift) == Keys.Shift ? -10 : -1);
                break;

            //case (Keys)(187):
            //case Keys.Add:
            case Keys.L:
                ChangeTableValue((ModifierKeys & Keys.Shift) == Keys.Shift ? 10 : 1);
                break;
        }
    }

    private void ChangeTableValue(int i)
    {
        var currentCol = rtGrid.CurrentCellAddress.X;
        var currentRow = rtGrid.CurrentCellAddress.Y;
        var colCount = rtGrid.ColumnCount;
        var index = currentRow * colCount + currentCol;

        if (gbcValue.Checked)
        {
            int value = _onlineManager.FirmwareManager.Gbc[currentCol, currentRow];
            value += (int)Math.Round((255 * i) / 100.0, MidpointRounding.AwayFromZero);
            value = Math.Min(Math.Max(0, value), 255);
            _onlineManager.FirmwareManager.Gbc.SetSource(index, (byte)value);
            _onlineManager.FirmwareManager.WriteGBCValue(index);
        }
        else if (veValue.Checked)
        {
            int value = _onlineManager.FirmwareManager.Kgbc_press[currentCol, currentRow];
            value += i;
            value = Math.Min(Math.Max(0, value), 255);
            _onlineManager.FirmwareManager.Kgbc_press.SetSource(index, (byte)value);
            _onlineManager.FirmwareManager.WriteKGBCValue(index);
        }
        else
        {
            int value = _onlineManager.FirmwareManager.Kgbc[currentCol, currentRow];
            value += i;
            value = Math.Min(Math.Max(0, value), 255);
            _onlineManager.FirmwareManager.Kgbc.SetSource(index, (byte)value);
            _onlineManager.FirmwareManager.WriteKGBCValue(index);
        }
    }

    private void SetCurrentCell(int index)
    {
        var colCount = rtGrid.ColumnCount;

        var tableRow = Math.DivRem(index, colCount, out var tableCol);
        rtGrid.CurrentCell = rtGrid.Rows[tableRow].Cells[tableCol];
        rtGrid.Refresh();
    }

    public void Prepare(OnlineManager onlineManager)
    {
        _onlineManager = onlineManager;
        onlineManager.settings.PropertyChanged += settings_PropertyChanged;
        disableFuelCutoff.Checked = onlineManager.settings.DisabledTHRZeroFuelCutoff;
    }

    private void TableValueChanged(object sender, ValueChangeArgs e) 
        => UpdateValues(e.Index);

    private void settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        disableFuelCutoff.Checked = e.PropertyName switch
        {
            "DisabledTHRZeroFuelCutoff" => _onlineManager.settings.DisabledTHRZeroFuelCutoff,
            _ => disableFuelCutoff.Checked
        };
    }

    private void InitGrid()
    {
        int[] cols;
        if (gbcValue.Checked)
        {
            cols = _onlineManager.FirmwareManager.RpmRt16;
        }
        else
        {
            if (_onlineManager.FirmwareManager.J7esFlags.IsKgbc32_16)
                cols = _onlineManager.FirmwareManager.RpmRt32;
            else if (veValue.Checked)
                cols = _onlineManager.FirmwareManager.J7esFlags.IsKgbcPress32_32 
                    ? _onlineManager.FirmwareManager.RpmRt32 
                    : _onlineManager.FirmwareManager.RpmRt16;
            else
                cols = _onlineManager.FirmwareManager.RpmRt16;
        }

        rtGrid.ColumnCount = cols.Length;

        var rows = veValue.Checked && !gbcValue.Checked ?
            _onlineManager.FirmwareManager.J7esFlags.IsKgbcPress32_32 
                ? _onlineManager.FirmwareManager.PressRt32 
                : _onlineManager.FirmwareManager.PressRt
            : _onlineManager.FirmwareManager.ThrRt;


        rtGrid.RowCount = rows.Length;
        if (!_onlineManager.FirmwareManager.IsOpened) return;

        for (var i = 0; i < rtGrid.RowCount; i++)
        {
            rtGrid.Rows[i].HeaderCell.Value = rows[i].ToString();
            rtGrid.Rows[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        for (var i = 0; i < rtGrid.ColumnCount; i++)
        {
            rtGrid.Columns[i].Width = 50;
            rtGrid.Columns[i].HeaderCell.Value = cols[i].ToString();
        }

        rtGrid.CurrentCell = rtGrid.Rows[0].Cells[0];
        rtGrid.ClientSize = new Size(rtGrid.ColumnCount * 50 + rtGrid.RowHeadersWidth + 5,
            rtGrid.RowCount * rtGrid.Rows[0].Height + rtGrid.ColumnHeadersHeight + 5);

        rtGridFillProgressBar.Location = new Point(rtGridFillProgressBar.Left, rtGrid.Bottom + 4);
        rtGridFillProgressBar.ClientSize = new Size(rtGrid.ClientSize.Width, rtGridFillProgressBar.ClientSize.Height);
    }

    public void DiagDataUpdate(DiagData diagData)
    {
        if (!_onlineManager.FirmwareManager.IsOpened || !_onlineManager.OltProtocol.Connected) return;

        var currentRow = veValue.Checked 
            ? _onlineManager.FirmwareManager.RpmPressRtIndex 
            : _onlineManager.FirmwareManager.ThrRtIndex;
        
        var currentCol = _onlineManager.FirmwareManager.RpmRtIndex;

        rpmThrRtIndex.Text =
            $"[{(veValue.Checked 
                ? _onlineManager.FirmwareManager.RpmPressRtIndex.ToString("X2") 
                : _onlineManager.FirmwareManager.RpmThrRtIndex.ToString("X2"))}]";

        int persent;
        if (gbcValue.Checked)
            persent = _onlineManager.FirmwareManager.Gbc.CalcFillPersent();
        else if (veValue.Checked)
            persent = _onlineManager.FirmwareManager.Kgbc.CalcFillPersent();
        else
            persent = _onlineManager.FirmwareManager.Kgbc_press.CalcFillPersent();

        rtGridFillProgressBar.Value = persent;
        thrRtPersent.Text = $"{persent}%";

        if (!_followRt) return;
        //select work dot 
        //@TODO: make separate selection and indicate
        rtGrid.InvalidateCell(currentCol, currentRow);
        rtGrid.CurrentCell = rtGrid.Rows[currentRow].Cells[currentCol];
    }

    private void UpdateValues(int index)
    {
        float value;
        int valueCol, valueRow;

        if (gbcValue.Checked)
        {
            valueRow = Math.DivRem(index, _onlineManager.FirmwareManager.Gbc.ColCount, out valueCol);
            value = _onlineManager.FirmwareManager.Gbc.GetValue(valueCol, valueRow);

            if (cbCorrectionParams.Checked)
            {
                var gbcCell = _onlineManager.FirmwareManager.Gbc.Cell(valueCol, valueRow);
                var valueStr = GetGbcCorrectionParams(gbcCell);
                rtGrid.Rows[valueRow].Cells[valueCol].Value = valueStr;
                rtGrid.Rows[valueRow].Cells[valueCol].ToolTipText = gbcCell.StopStudy 
                    ? "Точка обучена" 
                    : "Кол-во циклов обучения | Текущая ошибка | '+' - точка обучена";
            }
            else
            {
                rtGrid.Rows[valueRow].Cells[valueCol].Value = value;
            }
        }
        else if (veValue.Checked)
        {
            valueRow = Math.DivRem(index, _onlineManager.FirmwareManager.Gbc.ColCount, out valueCol);
            value = _onlineManager.FirmwareManager.Kgbc_press.GetValue(valueCol, valueRow);

            if (cbCorrectionParams.Checked)
            {
                var kgbcPressCell = _onlineManager.FirmwareManager.Kgbc_press.Cell(valueCol, valueRow);
                var valueStr = GetKGbcCorrectionParams(kgbcPressCell);
                rtGrid.Rows[valueRow].Cells[valueCol].Value = valueStr;
                rtGrid.Rows[valueRow].Cells[valueCol].ToolTipText = kgbcPressCell.StopStudy 
                    ? "Точка обучена" 
                    : "Кол-во циклов обучения | Текущая ошибка | '+' - точка обучена";
            }
            else
            {
                rtGrid.Rows[valueRow].Cells[valueCol].Value = value;
            }
        }
        else
        {
            valueRow = Math.DivRem(index, _onlineManager.FirmwareManager.Gbc.ColCount, out valueCol);
            value = _onlineManager.FirmwareManager.Kgbc.GetValue(valueCol, valueRow);

            if (cbCorrectionParams.Checked)
            {
                var kgbcCell = _onlineManager.FirmwareManager.Kgbc.Cell(valueCol, valueRow);
                var valueStr = GetKGbcCorrectionParams(kgbcCell);
                rtGrid.Rows[valueRow].Cells[valueCol].Value = valueStr;
                rtGrid.Rows[valueRow].Cells[valueCol].ToolTipText = kgbcCell.StopStudy ? "Точка обучена" : "Кол-во циклов обучения | Текущая ошибка | '+' - точка обучена";
            }
            else
            {
                rtGrid.Rows[valueRow].Cells[valueCol].Value = value;
            }
        }
        rtGrid.InvalidateCell(valueCol, valueRow);

        rtChart.PeData.Y[valueRow, valueCol] = value;
        Gigasoft.ProEssentials.Api.PEreconstruct3dpolygons(rtChart.PeSpecial.HObject);
        rtChart.PeFunction.ResetImage(0, 0);
        rtChart.Refresh();
    }

    private void pcnValue_CheckedChanged(object sender, EventArgs e)
    {
        if (sender is RadioButton { Checked: false }) return;

        LoadData();
    }

    public void InitData()
    {
        _table = GetTargetTable();

        InitGrid();

        _onlineManager.FirmwareManager.Gbc.ValueChanged += TableValueChanged;
        _onlineManager.FirmwareManager.Kgbc.ValueChanged += TableValueChanged;
        _onlineManager.FirmwareManager.Kgbc_press.ValueChanged += TableValueChanged;
    }

    public void LoadData()
    {
        _table = GetTargetTable();

        InitGrid();

        if (rtChart.Visible)
            LoadChart();
        else
            LoadGrid();
    }

    private ITableValues GetTargetTable()
    {
        if (veValue.Checked)
        {
            return gbcValue.Checked
                ? _onlineManager.FirmwareManager.Gbc
                : _onlineManager.FirmwareManager.Kgbc_press;
        }
        return gbcValue.Checked
            ? _onlineManager.FirmwareManager.Gbc
            : _onlineManager.FirmwareManager.Kgbc;
    }

    private void LoadChart()
    {
        var y = _table.GetFloatValues();
        float[] x;
        float[] z;
        var tableXsize = 16;
        var tableZsize = 16;
        if (veValue.Checked && !gbcValue.Checked || _onlineManager.FirmwareManager.J7esFlags.IsKgbc32_16 )
        {
            x = _onlineManager.FirmwareManager.Rpm32_16RtPoints;
            tableXsize = 32;

            if (_onlineManager.FirmwareManager.J7esFlags.IsKgbcPress32_32 || !gbcValue.Checked) {
                x = _onlineManager.FirmwareManager.Rpm32_32RtPoints;
                z = _onlineManager.FirmwareManager.PressRt32Points;
                tableXsize = 32;
                tableZsize = 32;
            }
            else 
            {
                z = _onlineManager.FirmwareManager.ThrRtPoints;
            }
        }
        else
        {
            x = _onlineManager.FirmwareManager.Rpm16_16RtPoints;
            z = _onlineManager.FirmwareManager.ThrRtPoints;                
        }


        PrepareChart(x.Length / tableXsize);

        Gigasoft.ProEssentials.Api.PEvsetW(rtChart.PeSpecial.HObject, Gigasoft.ProEssentials.DllProperties.XData,
            x, x.Length);

        Gigasoft.ProEssentials.Api.PEvsetW(rtChart.PeSpecial.HObject, Gigasoft.ProEssentials.DllProperties.YData,
            y, y.Length);

        Gigasoft.ProEssentials.Api.PEvsetW(rtChart.PeSpecial.HObject, Gigasoft.ProEssentials.DllProperties.ZData,
            z, z.Length);
        rtChart.PeFunction.ReinitializeResetImage();
        rtChart.Refresh();
    }

    private void PrepareChart(int count)
    {
        var vh = rtChart.PeUserInterface.Scrollbar.ViewingHeight;
        var dor = rtChart.PeUserInterface.Scrollbar.DegreeOfRotation;
        rtChart.PeFunction.Reset();
        rtChart.PeData.Subsets = count;
        rtChart.PeData.Points = count;

        rtChart.PeData.NullDataValue = rtChart.PeData.NullDataValueX = rtChart.PeData.NullDataValueZ = -9999;

        rtChart.PeColor.XZBackColor = Color.Empty;
        rtChart.PeColor.YBackColor = Color.Empty;

        // Mechanism to control polygon border color //
        rtChart.PeColor.BarBorderColor = Color.FromArgb(255, 0, 0, 0);
        rtChart.PeColor.SubsetColors[(int)SurfaceColors.WireFrame] = Color.FromArgb(96, 198, 0, 0);
        rtChart.PeColor.SubsetColors[(int)SurfaceColors.SolidSurface] = Color.FromArgb(96, 0, 148, 0);

        var palette = gbcValue.Checked ? _gbcPalette : _kgbcPalette;
        if (_onlineManager?.FirmwareManager?.Kgbc_press?.Min != null)
        {
            //throw new NotImplementedException("No min and max");
            var step = _onlineManager.FirmwareManager.Kgbc_press.xEnd - _onlineManager.FirmwareManager.Kgbc_press.xStart;
            //var step = 0.1;
            
            for (var subset = rtChart.PeData.Subsets + 1; subset > 0; subset--)
            {
                rtChart.PeColor.SubsetColors[subset] = Color.FromArgb(palette.GetColorOnValue((float)step * (subset), _backColor));
            }
        }

        // Alternately, if the above SubsetColor indices are not assigned.
        // (0) will be wireframe color for wireframe plotting method //
        // (0) will be solid color if plotting method is solid
        // (1) will be wire frame color for solid plotting methods

        // Set the plotting method //
        //! There are different plotting method values for each //
        //! case of PolyMode  //
        rtChart.PePlot.Method = ThreeDGraphPlottingMethod.Four;
        //rtChart.PeColor.ViewingStyle
        // Set various other properties //
        rtChart.PeColor.BitmapGradientMode = true;
        rtChart.PeColor.QuickStyle = QuickStyle.DarkLine;

        rtChart.PeLegend.SimplePoint = true;
        rtChart.PeLegend.SimpleLine = true;
        rtChart.PeLegend.Style = SimpleLegendStyle.OneLine;

        rtChart.PeString.MainTitle = gbcValue.Checked ? "БНЦ" : "ПНЦ";
        rtChart.PeString.SubTitle = "";
        rtChart.PeString.XAxisLabel = "rpm";
        rtChart.PeString.ZAxisLabel = "press/thr";
        rtChart.PeString.YAxisLabel = gbcValue.Checked ? "мг.цикл" : "coef";
        //rtChart.PeString.SubsetLabels[]

        rtChart.PeUserInterface.Scrollbar.ViewingHeight = vh;
        rtChart.PeUserInterface.Scrollbar.DegreeOfRotation = dor;
        rtChart.PeUserInterface.Scrollbar.MouseDraggingX = true;
        rtChart.PeUserInterface.Scrollbar.MouseDraggingY = true;

        // Set up cursor //
        //rtChart.PeUserInterface.Cursor.Hand = (int) MouseCursorStyles.Cross;
        rtChart.PeUserInterface.Cursor.ProcessingMouseMove = true;
        // Help see data points //
        rtChart.PePlot.MarkDataPoints = true;

        // This will allow you to move cursor by clicking data point //
        rtChart.PeUserInterface.HotSpot.Data = true;
        rtChart.PeUserInterface.HotSpot.Size = HotSpotSize.Large;

        rtChart.PeFont.Fixed = true;
        rtChart.PeFont.FontSize = FontSize.Medium;
        rtChart.PeConfigure.PrepareImages = false;
        rtChart.PeConfigure.CacheBmp = false;
        rtChart.PeUserInterface.Allow.FocalRect = false;
        rtChart.PePlot.Option.ShadingStyle = ShadingStyle.White;
        rtChart.PePlot.Option.ShowBoundingBox = ShowBoundingBox.Never;

        rtChart.PeConfigure.TextShadows = TextShadows.BoldText;
        rtChart.PeFont.MainTitle.Bold = true;
        rtChart.PeFont.SubTitle.Bold = true;
        rtChart.PeFont.Label.Bold = true;

        // Improves Metafile Export //
        rtChart.PeSpecial.DpiX = 600;
        rtChart.PeSpecial.DpiY = 600;


        rtChart.PeConfigure.RenderEngine = RenderEngine.Hybrid;
        rtChart.PeConfigure.AntiAliasGraphics = false;
        rtChart.PeConfigure.AntiAliasText = false;
    }

    private void LoadGrid()
    {
        ((ISupportInitialize)rtGrid).BeginInit();
        rtGrid.ClearSelection();

        try
        {
            if (gbcValue.Checked)
                LoadGBCData();
            else
                LoadKGBCData();
        }
        finally
        {
            ((ISupportInitialize)rtGrid).EndInit();
        }

        rtGrid.Refresh();
    }

    private void LoadKGBCData()
    {
        var tableKGBC = (TableValues<byte, float>)_table;
        if (cbCorrectionParams.Checked)
        {
            for (var i = 0; i < rtGrid.RowCount; i++)
            {
                var row = rtGrid.Rows[i];

                for (var j = 0; j < rtGrid.ColumnCount; j++)
                {
                    row.Cells[j].Value = tableKGBC.GetValue(j, i);
                    var kgbcCell = tableKGBC.Cell(j, i);
                    var valueStr = GetKGbcCorrectionParams(kgbcCell);
                    row.Cells[j].Value = valueStr;
                    row.Cells[j].ToolTipText = "Кол-во циклов обучения|Текущая ошибка|'+' - точка обучена";
                }
            }
        }
        else
        {
            for (var i = 0; i < rtGrid.RowCount; i++)
            {
                var row = rtGrid.Rows[i];

                for (var j = 0; j < rtGrid.ColumnCount; j++)
                {
                    row.Cells[j].Value = tableKGBC.GetValue(j, i);
                    row.Cells[j].ToolTipText = string.Empty;
                }
            }
        }
    }

    private void LoadGBCData()
    {
        var tableGBC = (TableValues<byte, short>)_table;

        if (cbCorrectionParams.Checked)
        {
            for (var i = 0; i < rtGrid.RowCount; i++)
            {
                var row = rtGrid.Rows[i];

                for (var j = 0; j < rtGrid.ColumnCount; j++)
                {
                    row.Cells[j].Value = tableGBC.GetValue(j, i);
                    var gbcCell = tableGBC.Cell(j, i);
                    var valueStr = GetGbcCorrectionParams(gbcCell);
                    row.Cells[j].Value = valueStr;
                    row.Cells[j].ToolTipText = "Кол-во циклов обучения | Текущая ошибка | '+' - точка обучена";
                }
            }
        }
        else
        {
            for (var i = 0; i < rtGrid.RowCount; i++)
            {
                var row = rtGrid.Rows[i];

                for (var j = 0; j < rtGrid.ColumnCount; j++)
                {
                    row.Cells[j].Value = tableGBC.GetValue(j, i);
                    row.Cells[j].ToolTipText = string.Empty;
                }
            }
        }
    }

    private static string GetKGbcCorrectionParams(TableCell<byte, float> kgbcCell)
    {
        var valueStr = $"{kgbcCell.Count}|{kgbcCell.Error.ToString("0.###")}|{(kgbcCell.StopStudy ? "+" : "-")}";
        return valueStr;
    }

    private static string GetGbcCorrectionParams(TableCell<byte, short> gbcCell)
    {
        var valueStr = $"{gbcCell.Count}|{gbcCell.Error.ToString("0.###")}|{(gbcCell.StopStudy ? "+" : "-")}";
        return valueStr;
    }

    private void disableFuelCutoff_Click(object sender, EventArgs e) 
        => _onlineManager.settings.DisabledTHRZeroFuelCutoff = disableFuelCutoff.Checked;

    private void rtGrid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.Value == null) return;

        var colCount = rtGrid.ColumnCount;
        var rowCount = rtGrid.RowCount;
        var cellCount = colCount * rowCount;

        var currentIndex = rtGrid.CurrentCellAddress.Y * colCount + rtGrid.CurrentCellAddress.X;

        var cellIndex = e.RowIndex * colCount + e.ColumnIndex;
        
        if (cellIndex == currentIndex)
            e.CellStyle.BackColor = Color.Cyan;
        else
        {
            if (cbCorrectionParams.Checked)
            {
                var color = Color.LightGoldenrodYellow;

                if (cellIndex >= 0 && cellIndex < cellCount)
                {
                    if (gbcValue.Checked)
                    {
                        var cell = _onlineManager.FirmwareManager.Gbc.Cell(cellIndex);
                        color = cell.Count > 0
                            ? Color.FromArgb(_gbcStudyPalette.GetColorOnValue(cell.Error, _backColor))
                            : color;
                        color = cell.StopStudy ? Color.LimeGreen : color;
                    }
                    else if (veValue.Checked)
                    {
                        var cell = _onlineManager.FirmwareManager.Kgbc_press.Cell(cellIndex);
                        color = cell.Count > 0
                            ? Color.FromArgb(_kgbcStudyPalette.GetColorOnValue(cell.Error, _backColor))
                            : color;
                        color = cell.StopStudy ? Color.LimeGreen : color;
                    }
                    else
                    {
                        var cell = _onlineManager.FirmwareManager.Kgbc.Cell(cellIndex);
                        color = cell.Count > 0
                            ? Color.FromArgb(_kgbcStudyPalette.GetColorOnValue(cell.Error, _backColor))
                            : color;
                        color = cell.StopStudy ? Color.LimeGreen : color;
                    }
                }

                e.CellStyle.BackColor = color;
            }
            else
            {
                var palette = gbcValue.Checked ? _gbcPalette : _kgbcPalette;
                if (palette.Palette.Count == 0) return;

                if (float.TryParse(e.Value.ToString(), out var value))
                    e.CellStyle.BackColor = Color.FromArgb(palette.GetColorOnValue(value, _backColor));
            }
        }
    }

    private void btnGridChart_Click(object sender, EventArgs e)
    {
        rtChart.Visible = !rtChart.Visible;
        rtGrid.Visible = !rtChart.Visible;
        LoadData();
    }

    private void btnSmoothing_Click(object sender, EventArgs e)
    {
        var table = gbcValue.Checked
            ? _onlineManager.FirmwareManager.Gbc.Get2DArray()
            : veValue.Checked 
                ? _onlineManager.FirmwareManager.Kgbc_press.Get2DArray() 
                : _onlineManager.FirmwareManager.Kgbc.Get2DArray();


        if (!byte.TryParse(smoothingFactor.Text, out var factor))
            return;

        var smoothing = DataHelper.Smoothing(table, factor);
        if (gbcValue.Checked)
        {
            _onlineManager.FirmwareManager.Gbc.Set2DArray(smoothing);
            _onlineManager.FirmwareManager.Gbc.DoTableChanged();
        }
        else if (pcnValue.Checked)
        {
            if (veValue.Checked)
            {
                _onlineManager.FirmwareManager.Kgbc_press.Set2DArray(smoothing);
                _onlineManager.FirmwareManager.Kgbc_press.DoTableChanged();
            }
            else
            {
                _onlineManager.FirmwareManager.Kgbc.Set2DArray(smoothing);
                _onlineManager.FirmwareManager.Kgbc.DoTableChanged();
            }

        }

        LoadData();
    }

    private void cbxFollowRT_CheckedChanged(object sender, EventArgs e) 
        => _followRt = cbxFollowRT.Checked;

    private void rtChart_MouseMove(object sender, MouseEventArgs e)
    {
        var pt = rtChart.PeUserInterface.Cursor.LastMouseMove;
        var pX = pt.X;
        var pY = pt.Y;

        // Calls to fill hot spot data structure with hot spot data at given x and y
        rtChart.PeFunction.GetHotSpot(pX, pY);

        // Calls PEgethotspot //
        var ds = rtChart.PeFunction.GetHotSpotData();
        if (ds.Type != HotSpotType.DataPoint) return;

        var extra1 = ds.Data1;
        var extra2 = ds.Data2;
        hotSpotValue.Text = $"[{extra1}, {extra2}]";
    }

    private void rtGrid_ColumnAdded(object sender, DataGridViewColumnEventArgs e) 
        => e.Column.SortMode = DataGridViewColumnSortMode.NotSortable;

    private void rtGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
#if DEBUG            
        if (gbcValue.Checked)
        {
            var cellIndex = e.RowIndex * _onlineManager.FirmwareManager.Gbc.ColCount + e.ColumnIndex;
            var tableCell = _onlineManager.FirmwareManager.Gbc.Cell(cellIndex);
            tableCell.Count++;
            tableCell.Error += 10;
            tableCell.StopStudy = tableCell.Count > 10;
            UpdateValues(cellIndex);
        }
        else if (veValue.Checked)
        {
            var cellIndex = e.RowIndex * _onlineManager.FirmwareManager.Kgbc_press.ColCount + e.ColumnIndex;
            var tableCell = _onlineManager.FirmwareManager.Kgbc_press.Cell(cellIndex);
            tableCell.Count++;
            tableCell.Error += 0.1f;
            tableCell.StopStudy = tableCell.Count > 10;
            UpdateValues(cellIndex);
        }
        else
        {
            var cellIndex = e.RowIndex * _onlineManager.FirmwareManager.Kgbc.ColCount + e.ColumnIndex;
            var tableCell = _onlineManager.FirmwareManager.Kgbc.Cell(cellIndex);
            tableCell.Count++;
            tableCell.Error += 0.1f;
            tableCell.StopStudy = tableCell.Count > 10;
            UpdateValues(cellIndex);
        }
#endif
    }

    private void veValue_CheckStateChanged(object sender, EventArgs e)
    {
        if (sender is RadioButton { Checked: false }) return;
        
        _onlineManager.FirmwareManager.IsVolumetricEfficiency = veValue.Checked;
        LoadData();
    }
}