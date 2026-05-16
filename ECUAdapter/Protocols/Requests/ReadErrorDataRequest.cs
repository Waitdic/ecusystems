using System;
using System.Collections.Generic;

namespace EcuCommunication.Protocols.Requests;

public sealed class ReadErrorDataRequest : JRequest
{        
    public readonly List<ECUError> errors = [];

    public ReadErrorDataRequest() : base("8410F11800FF009C")
    {
    }

    protected override void DoExecute(EventArgs e)
    {
        base.DoExecute(e);

        if (Test())
            ParseValues();
    }

    private void ParseValues()
    {
        errors.Clear();
        var valueOffset = replyValueOffset;
        var count = replyBuffer[valueOffset++];

        if (valueOffset + count*3 + 1 > replyBuffer.Length) return;
            
        for (var i = 0; i < count; i++)
        {
            var code = (ushort)((replyBuffer[valueOffset++] << 8) + replyBuffer[valueOffset++]);
            var status = replyBuffer[valueOffset++];
                
            errors.Add(ECUErrorFactory.CreateECUError(code, status));
        }
    }
}