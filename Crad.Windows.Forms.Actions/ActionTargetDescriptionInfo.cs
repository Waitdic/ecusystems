using System;
using System.Reflection;
using System.Collections.Generic;

namespace Crad.Windows.Forms.Actions;

public class ActionTargetDescriptionInfo
{
    public ActionTargetDescriptionInfo(Type targetType)
    {
        properties = new Dictionary<string,PropertyInfo>();
        this.targetType = targetType;

        foreach (var property in targetType.GetProperties())
            properties.Add(property.Name, property);
    }

    private Dictionary<string, PropertyInfo> properties;

    private Type targetType;
    public Type TargetType => targetType;

    internal void SetValue(string propertyName, object target, object value)
    {
        if (properties.ContainsKey(propertyName))
            properties[propertyName].SetValue(target, value, null);
    }

    internal object GetValue(string propertyName, object source) 
        => properties.ContainsKey(propertyName) 
            ? properties[propertyName].GetValue(source, null) 
            : null;
}
