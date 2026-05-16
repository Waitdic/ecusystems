using System.ComponentModel;

namespace Helper;

public enum EnBaundRate
{
    [Description("10400")]
    Low,
    [Description("38400 (Январь 7/5)")]
    Medium,
    [Description("57600 (Только Январь 5)")]
    Hi
}
