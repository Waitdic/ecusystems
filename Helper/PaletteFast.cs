using System;
using System.Drawing;

namespace Helper;

public class PaletteFast(Palette palette, int scale = 1)
{
    private int[] _colors;
    public Palette Palette { get; } = palette;

    private int _min;
    private int _max;
    private int _minColor;
    private int _maxColor;

    public int Scale { get; } = scale;

    public void FillColors()
    {
        _min = (int)Math.Floor(Palette.GetValue(0) * Scale);
        _max = (int)Math.Ceiling(Palette.GetValue(Palette.Count - 1) * Scale);
        _minColor = Palette.GetColor(0);
        _maxColor = Palette.GetColor(Palette.Count - 1);
        
        var count = _max - _min + 1;
        _colors = new int[count];
        
        var defaultColor = Color.Transparent.ToArgb();
        
        for (var i = 0; i < _colors.Length; ++i)
            _colors[i] = Palette.GetColorOnValue((_min + i) / (float)Scale, defaultColor);
    }        

    public int GetColorOnValue(float value, int defaultColor)
    {
        var scaleValue = (int) Math.Round(value * Scale, MidpointRounding.AwayFromZero);

        if (scaleValue > _max)
            return Palette.LimitAbove ? defaultColor : _maxColor;
        
        if (scaleValue < _min)
            return Palette.LimitBelow ? defaultColor : _minColor;

        return _colors[scaleValue - _min];
    }

    public void Clear()
    {
        Palette.Clear();
        _colors = [];
    }
}
