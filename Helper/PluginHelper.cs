using System;

namespace Helper;

[AttributeUsage(AttributeTargets.Assembly)]
public class PluginAttribute(string name, string description) : Attribute
{
    public string Name { get; private set; } = name;
    public string Description { get; private set; } = description;
}
