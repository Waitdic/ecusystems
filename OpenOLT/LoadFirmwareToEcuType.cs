using System.ComponentModel;

namespace OpenOLT;

public enum LoadFirmwareToEcuType
{
    [Description("Только корректируемые таблицы")]
    OnlyCorrectionTable,
    [Description("Всю область калибровок")]
    FullLoad,
    [Description("Ничего")]
    None
}