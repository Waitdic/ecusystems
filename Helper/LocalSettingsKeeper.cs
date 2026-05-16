using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace Helper;

public class LocalSettingsKeeper
{
    private IDictionary<string, string> _settings;

    public void LoadSettings()
    {
        try
        {
            var path = BuildPath();
            if (!File.Exists(path))
            {
                _settings = new Dictionary<string, string>();
                return;
            }

            using var fs = new FileStream(path, FileMode.Open);
            var formatter = new BinaryFormatter();
            _settings = (IDictionary<string, string>) formatter.Deserialize(fs);
        }
        catch
        {
            _settings = new Dictionary<string, string>();
        }
    }

    public void LoadSettings(object target)
    {
        LoadSettings();

        foreach (var property in target.GetType().GetProperties())
        {
            try
            {
                var value = GetValue(property.Name, property.GetValue(target, null));
                property.SetValue(target,
                                  property.PropertyType.IsEnum
                                      ? Enum.Parse(property.PropertyType, value.ToString())
                                      : Convert.ChangeType(value, property.PropertyType), null);
            }
            catch
            {
                // ignored
            }
        }
    }

    private static string BuildPath()
    {
        return Application.StartupPath + @"\settings.dat";
    }

    public void SaveSettings()
    {
        var path = BuildPath();
        if (!Directory.Exists(Path.GetDirectoryName(path)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));                
        }

        using var fs = new FileStream(path, FileMode.OpenOrCreate);
        var formatter = new BinaryFormatter();
        formatter.Serialize(fs, _settings);
    }

    public void SaveSettings(object target)
    {
        foreach (var property in target.GetType().GetProperties())
        {
            try
            {
                var value = property.GetValue(target, null);
                SetValue(property.Name, value);
            }
            catch
            {
                // ignored
            }
        }

        SaveSettings();
    }

    public T GetValue<T>(string name, T defValue)
    {
        return _settings.TryGetValue(name, out var setting) 
            ? (T) Convert.ChangeType(setting, typeof (T)) 
            : defValue;
    }

    public void SetValue<T>(string name, T value) 
        => _settings[name] = value.ToString();
}