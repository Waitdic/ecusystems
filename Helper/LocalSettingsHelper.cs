using System.Collections.Generic;

namespace Helper;

public static class LocalSettingsHelper
{
    private const string CountPattern = "{0}_COUNT";        

    public static void SetValues<T>(LocalSettingsKeeper valueSaver, string name, T[] values)
    {
        var i = 0;
        var baseName = name + "_{0}";
        var countName = string.Format(CountPattern, name);
        valueSaver.SetValue(countName, values.Length);

        foreach (var value in values)
        {
            valueSaver.SetValue(string.Format(baseName, i++), value);
        }
    }

    public static IEnumerable<T> GetValues<T>(LocalSettingsKeeper valueLoader, string name)
    {            
        var baseName = name + "_{0}";
        var countName = string.Format(CountPattern, name);
        var count = valueLoader.GetValue(countName, 0);
        var values = new T[count];

        for (var i = 0; i < count; i++)
        {
            values[i] = valueLoader.GetValue(string.Format(baseName, i), default(T));
        }

        return values;
    }
}