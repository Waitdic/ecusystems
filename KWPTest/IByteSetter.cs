using System;

namespace KWPTest;

public interface IByteSetter
{
    byte Value { get; }
    event EventHandler OnValueChange;
}