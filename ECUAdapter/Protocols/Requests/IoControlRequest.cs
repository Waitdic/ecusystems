using Helper;

namespace EcuCommunication.Protocols.Requests;

internal class IoControlRequest: JRequest
{
    public void StopCapture(byte id)
    {
        requestBuffer = DataHelper.StrToByteArray($"8310F130{id.ToString("X2")}0000");
        CalcCRC();
    }

    public void StartCaptureAndSetValue(byte id, byte value)
    {
        requestBuffer = DataHelper.StrToByteArray($"8410F130{id.ToString("X2")}07{value.ToString("X2")}00");
        CalcCRC();
    }       
}