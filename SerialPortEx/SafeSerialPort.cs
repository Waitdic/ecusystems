using System;
using System.IO;
using System.IO.Ports;

namespace SerialPortEx;

public class SafeSerialPort : SerialPort
{
    private Stream _theBaseStream;

    public SafeSerialPort(
        string portName,
        int baudRate,
        Parity parity,
        int dataBits,
        StopBits stopBits)
        : base(portName, baudRate, parity, dataBits, stopBits)
    {
    }

    public new void Open()
    {
        try
        {
            base.Open();
            _theBaseStream = BaseStream;
            GC.SuppressFinalize(BaseStream);
        }
        catch
        {
            // ignored
        }
    }

    public new void Dispose()
    {
        Dispose(true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && Container != null)
        {
            Container.Dispose();
        }
        try
        {
            if (_theBaseStream.CanRead)
            {
                _theBaseStream.Close();
                GC.ReRegisterForFinalize(_theBaseStream);
            }
        }
        catch
        {
            // ignore exception - bug with USB - serial adapters.
        }
        
        base.Dispose(disposing);
    }

    public new void DiscardInBuffer() 
    {
        try
        {
            base.DiscardInBuffer();               
        }
        catch
        {
            // ignore exception - bug with USB - serial adapters.
        }

    }

    public new void DiscardOutBuffer()
    {
        try
        {
            base.DiscardOutBuffer();
        }
        catch
        {
            // ignore exception - bug with USB - serial adapters.
        }
    }
}