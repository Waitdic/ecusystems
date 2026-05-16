using System;

namespace KWPTest;

public interface IWordSetter
{
    ushort Value { get; set; }
    byte Byte1 { get; }
    byte Byte2 { get; }
    event EventHandler OnValueChange;
}