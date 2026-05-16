using System.ComponentModel;

namespace OpenOltTypes;

public class AutoCorrectionEventArgs(int address, int index, byte source) : CancelEventArgs
{
    public int Address { get; set; } = address;
    public int Index { get; private set; } = index;
    public byte Source { get; private set; } = source;
}