using System;
using Helper;

namespace EcuCommunication.Protocols.Requests;

class ReadRamRequest: JRequest
{
    public byte[] value;
    private int _length;

    public void Prepare(int address, int count)
    {
        value = null;
        _length = count;            
        requestBuffer =
            DataHelper.StrToByteArray($"8510F12300{(address & 0xFFFF).ToString("X4")}{(count & 0xFF).ToString("X2")}00");
        
        CalcCRC();
    }

    protected override bool TestRequestEcho() => true;

    protected override void DoExecute(EventArgs e)
    {
        base.DoExecute(e);

        if (Test())
            ParseValues();
    }

    private void ParseValues()
    {
        var valueOffset = replyValueOffset;
        if (valueOffset + _length >= replyBuffer.Length) return;
        
        value = new byte[_length];
        Array.Copy(replyBuffer, valueOffset, value, 0, _length);
    }
}