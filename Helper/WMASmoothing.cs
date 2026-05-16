namespace Helper;

public class WMASmoothing(byte window)
{
    private byte _windows;
    private ulong count;
    private float[] _values = new float[window];
    public float Value { get; }

    public void Add(float value) {}
}
