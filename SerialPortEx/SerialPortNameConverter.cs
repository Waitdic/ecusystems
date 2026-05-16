using System;
using System.ComponentModel;
using System.Globalization;
using System.IO.Ports;

namespace SerialPortEx;

public class SerialPortNameConverter : TypeConverter
{
    // Fields
    private static StandardValuesCollection values;

    // Methods
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
    {            
        return values ??= new StandardValuesCollection(SerialPort.GetPortNames());
    }        

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is not string) 
            return base.ConvertFrom(context, culture, value);
        
        return ((string)value).Trim();
    }       

    public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;
    
    public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
}