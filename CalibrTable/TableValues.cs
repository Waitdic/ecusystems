using System;
using System.ComponentModel;
using System.Linq;

namespace CalibrTable;

public class TableValues<TSource, TValue> : ITableValues where  TSource: struct, IComparable<TSource> where TValue: struct, IComparable<TValue>
{
    public object Tag { get; set; }

    private int _colCount;
    public int ColCount => _colCount;
    private int _rowCount;
    public int RowCount => _rowCount;
    private int _count;
    public int Count => _count;
    private TableCell<TSource, TValue>[] _sources;
    private TValue[] _values;
    public Func<TSource, TValue> converter;
    public Func<TValue, TSource> reverseConverter;
    public int Address { get; set; }
    public int FillThreshold { get; set; }

    private int currentIndex;
    
    public int CurrentIndex
    {
        get => currentIndex;
        set
        {
            if (currentIndex == value) return;
            currentIndex = value;
            DoPropertyChanged(new PropertyChangedEventArgs("CurrentIndex"));
        }
    }

    public float[] AxisY { get; set; }
    
    public float ConvertRawToValue(int raw)
    {
        return Convert.ToSingle(converter((TSource) Convert.ChangeType(raw, typeof (TSource))));
    }

    public float[] AxisX { get; set; }        
    public event EventHandler<ValueChangeArgs> ValueChanged; 

    public TValue CurrentValue { get { return _values != null ? _values[CurrentIndex] : default(TValue); } }
    public TSource CurrentRawValue { get { return _sources != null ? _sources[CurrentIndex].Source : default(TSource); } }

    public TSource this[int index] 
    { 
        get => _sources[index].Source;
        set
        {
            var cell = _sources[index];
            cell.OldSource = cell.Source;
            cell.Source = value;                
            cell.Count++;
        }
    }

    public TSource this[int col, int row]
    {
        get => _sources[col + row*_colCount].Source;
        set
        {
            var cell = _sources[col + row * _colCount];
            cell.OldSource = cell.Source;
            cell.Source = value;
            cell.Count++;
        }
    }

    public TableCell<TSource, TValue> Cell(int index) 
        => _sources[index];

    public TableCell<TSource, TValue> Cell(int col, int row) 
        => _sources[col + row * _colCount];

    public TValue GetValue(int index) 
        => _values?[index] ?? default;

    public TValue GetValue(int col, int row) 
        => _values?[col + row*_colCount] ?? default;

    public float GetFloatValue(int index) 
        => _values == null ? Convert.ToSingle(_sources[index]) : Convert.ToSingle(_values[index]);

    public float GetFloatValue(int col, int row) 
        => _values == null 
            ? Convert.ToSingle(_sources[col + row * _colCount]) 
            : Convert.ToSingle(_values[col + row * _colCount]);

    public int GetRawValue(int col, int row) 
        => Convert.ToInt32(_sources[col + row * _colCount].Source);

    public void SetRawValue(int col, int row, int value) 
        => SetSource(col + row*_colCount, (TSource) Convert.ChangeType(value, typeof (TSource)));

    public void SetValue(int index, TValue value)
    {
        var rawValue = reverseConverter(value);
        this[index] = rawValue;

        _values?[index] = converter(rawValue);

        LastEditCellIndex = index;

        DoValueChange(index);
    }

    public void SetValue(int col, int row, TValue value) 
        => SetValue(col + row * _colCount, value);

    public void SetFloatValue(int index, float value) 
        => SetValue(index, (TValue)Convert.ChangeType(value, typeof(TValue)));

    public void SetFloatValue(int col, int row, float value) 
        => SetValue(col, row, (TValue)Convert.ChangeType(value, typeof(TValue)));

    public int LastEditCellIndex { get; private set; }

    public float Min { get; set; }
    public float Max { get; set; }

    public int RawMin { get; set; }
    public int RawMax { get; set; }

    public string Name { get; set; }

    public void DoValueChange(int index) 
        => ValueChanged?.Invoke(this, new ValueChangeArgs {Index = index});

    public void SetSource(int index, TSource source)
    {
        _sources[index].Source = source;
        _values[index] = converter(source);
        DoValueChange(index);
    }

    public TableValues() {}

    public TableValues(
        int address,
        int col, 
        int row,
        Func<TSource, TValue> converter = null,
        Func<TValue, TSource> reverseConverter = null)
    {
        Address = address;
        Init(col, row, converter, reverseConverter, null);
    }

    public void Init(
        int col,
        int row,
        Func<TSource, TValue> converter,
        Func<TValue, TSource> reverseConverter,
        TSource[] source)
    {
        _colCount = col;
        _rowCount = row;
        _count = _colCount * _rowCount;
        _sources = new TableCell<TSource, TValue>[_count];
        _values = new TValue[_count];

        for (var i = 0; i < _count; i++)
        {
            _sources[i] = new TableCell<TSource, TValue>();
            if (source != null)
                _sources[i].Source = source[Address + i];
        }
        this.converter = converter;
        this.reverseConverter = reverseConverter;
    }

    public int CalcAddress(int index) => Address + index;

    public int CalcFillPersent()
    {
        if (_count == 0) return 0;

        var persent = 0.0;
        for (var i = 0; i < _count; i++)
        {
            var cell = _sources[i];
            if (!cell.StopStudy) continue;

            persent += 1;
        }

        return (int)Math.Round(persent/_count*100, MidpointRounding.AwayFromZero);
    }

    public void FillValues()
    {
        if (converter == null) return;
        
        for (var i = 0; i < _sources.Length; i++)
        {
            var cell = _sources[i];
            var value = converter(cell.Source);
            cell.StudyValue = value;
            cell.Value = value;
            _values[i] = value;
        }
    }

    public void FirstInit()
    {
        for (var i = 0; i < _count; i++)
        {
            var cell = _sources[i];
            cell.OldSource = cell.Source;
            cell.FirstSource = cell.Source;
            cell.Count = 0;
            cell.Error = cell.E_1 = cell.E_2 = default;
            cell.StopStudy = false;
            cell.Tag = 0;
        }
    }

    public float[] GetFloatValues()
    {
        var count = _colCount * _rowCount;
        var data = new float[count];

        if (_values != null)
            for (var i = 0; i < count; i++)
                data[i] = Convert.ToSingle(_values[i]);
        else
            for (var i = 0; i < count; i++)
                data[i] = Convert.ToSingle(_sources[i].Source);

        return data;
    }

    public void SetFloatValues(float[] source)
    {
        if (_values.Length != _count) throw new IndexOutOfRangeException();

        for (var i = 0; i < _count; i++)
        {
            var value = (TValue)Convert.ChangeType(source[i], typeof(TValue));

            _values?[i] = value;

            var rawValue = reverseConverter(value);
            this[i] = rawValue;
        }

        DoTableChanged();
    }
    
    public TSource[] GetRawBuffer() 
        => (from item in _sources select item.Source).ToArray();

    public void SetRawBuffer(TSource[] buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
            _sources[i].Source = buffer[i];
    }

    public void DoTableChanged() 
        => DoPropertyChanged(new PropertyChangedEventArgs("Table"));

    public event PropertyChangedEventHandler PropertyChanged;

    private void DoPropertyChanged(PropertyChangedEventArgs e) 
        => PropertyChanged?.Invoke(this, e);

    public string xUnits { get; set; }
    public string Units { get; set; }
    public double xStart { get; set; }
    public double xEnd { get; set; }
    public ushort xPoints { get; set; }
}
