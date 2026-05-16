using System;
using System.ComponentModel;
using System.Globalization;

namespace Helper;

/// <summary>
/// TypeConverter для Boolean, преобразовывающий Enum к строке с учетом атрибута Description
/// </summary>
public class BaseBooleanTypeConverter : BooleanConverter
{
    private readonly string _mTrue;
    private readonly string _mFalse;

    protected BaseBooleanTypeConverter(string strTrue, string strFalse)
    {
        _mTrue = strTrue;
        _mFalse = strFalse;
    }

    public override object ConvertTo(
        ITypeDescriptorContext context,
        CultureInfo culture,
        object value,
        Type destType) => (bool)value ? _mTrue : _mFalse;

    public override object ConvertFrom(
        ITypeDescriptorContext context,
        CultureInfo culture,
        object value) => (string)value == _mTrue;
}

/// <summary>
/// TypeConverter для Enum, преобразовывающий Enum к строке с учетом атрибута Description
/// </summary>
public class EnumTypeConverter : EnumConverter
{
    private readonly Type _enumType;

    /// <summary>Инициализирует экземпляр</summary>
    /// <param name="type">тип Enum</param>
    public EnumTypeConverter(Type type) : base(type)
    {
        _enumType = type;
    }

    public override bool CanConvertTo(
        ITypeDescriptorContext context,
        Type destType) => destType == typeof(string);

    public override object ConvertTo(ITypeDescriptorContext context,
      CultureInfo culture,
      object value, Type destType)
    {
        var fi = _enumType.GetField(Enum.GetName(_enumType, value));
        
        var dna = (DescriptionAttribute)Attribute.GetCustomAttribute(
            fi, typeof(DescriptionAttribute));

        return dna == null ? value.ToString() : dna.Description;
    }

    public override bool CanConvertFrom(
        ITypeDescriptorContext context,
        Type srcType) => srcType == typeof(string);

    public override object ConvertFrom(
        ITypeDescriptorContext context,
        CultureInfo culture,
        object value)
    {
        foreach (var fi in _enumType.GetFields())
        {
            var dna = (DescriptionAttribute)Attribute.GetCustomAttribute(
                fi, typeof(DescriptionAttribute));

            if (dna != null && (string)value == dna.Description)
                return Enum.Parse(_enumType, fi.Name);
        }

        return Enum.Parse(_enumType, (string)value);
    }
}

public class BooleanTypeConverter : BaseBooleanTypeConverter
{
    private BooleanTypeConverter() : base("Да", "Нет") { }
}

public class BoolEventArgs(bool answer) : EventArgs
{
    public bool BoolAnswer { get; private set; } = answer;
}

public class UintHexTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(
        ITypeDescriptorContext context,
        Type sourceType) 
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override bool CanConvertTo(
        ITypeDescriptorContext context,
        Type destinationType) 
        => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    public override object ConvertTo(
        ITypeDescriptorContext context,
        CultureInfo culture,
        object value,
        Type destinationType)
    {
        return destinationType == typeof (string) && value is uint
            ? $"0x{value:X8}"
            : base.ConvertTo(context, culture, value, destinationType);
    }

    public override object ConvertFrom(
        ITypeDescriptorContext context,
        CultureInfo culture,
        object value)
    {
        if (value is not string s) 
            return base.ConvertFrom(context, culture, value);
        
        var input = s;

        if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            input = input.Substring(2);
        }

        return uint.Parse(input, NumberStyles.HexNumber, culture);
    }
} 
