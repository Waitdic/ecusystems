using System.Linq;
using System.Windows.Forms;
using Helper;

namespace SokolovSport.Options;

static class OptionsHelper
{
    public static OptionsEntity Options { get; }

    private static readonly LocalSettingsKeeper SettingsKeeper;

    static OptionsHelper()
    {         
        Options = new OptionsEntity();
        SettingsKeeper = new LocalSettingsKeeper();
        SettingsKeeper.LoadSettings(Options);
        var datFiles = LocalSettingsHelper.GetValues<string>(SettingsKeeper, "OPENED_DAT_FILE");
        Options.OpenedDatFile.AddRange(datFiles);
    }

    public static void OpenDialog(IWin32Window owner) 
        => SettingsHelper.ShowSettingsDialog(owner, Options);

    public static void SaveSettings()
    {
        LocalSettingsHelper.SetValues(SettingsKeeper, "OPENED_DAT_FILE", Options.OpenedDatFile.Take(20).ToArray());
        SettingsKeeper.SaveSettings(Options);
    }
}