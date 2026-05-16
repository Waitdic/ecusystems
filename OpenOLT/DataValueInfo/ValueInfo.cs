using System;
using System.ComponentModel;
using System.Reflection;

namespace OpenOLT.DataValueInfo;

internal class ValueInfo(string title, float min, float max, string name, int order)
{
    public string Title { get; } = title;
    public float Min { get; set; } = min;
    public float Max { get; set; } = max;
    public string Name { get; } = name;
    public int Order { get; } = order;

    public ValueInfo(PropertyInfo info) 
        : this(((DisplayNameAttribute) Attribute.GetCustomAttribute(
            info,
            typeof (DisplayNameAttribute))).DisplayName,
            0,
            0,
            info.Name,
            0)
    {
    }

    public override string ToString() => Title;
}