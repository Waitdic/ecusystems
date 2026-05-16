using System;
using Helper;

namespace EcuCommunication.Protocols.Requests;

internal sealed class OltDiagV3DataRequest : JRequest, IDiagDataRequest
{
    public DiagData DiagData { get; }

    public OltDiagV3DataRequest() : base("8210F1210FB3")
    {
        testCrc = false;
        isOnline = true;
        DiagData = new DiagData();
    }

    protected override bool TestRequestEcho() => true;

    protected override void DoExecute(EventArgs e)
    {
        base.DoExecute(e);

        if (Test())
        {
            ParseValues();
        }            
    }

    private void ParseValues()
    {
        var valueOffset = replyValueOffset + 1;
        if (valueOffset + 49 > replyBuffer.Length) return;

        DiagData.TRT = replyBuffer[valueOffset + 7];
        DiagData.RPM = (ushort)(5000000f / ((replyBuffer[valueOffset + 10] << 8) + replyBuffer[valueOffset + 9]));
        DiagData.RPM40 = (ushort)(replyBuffer[valueOffset + 8] * 40);
        DiagData.UOZ = (sbyte)(((sbyte)replyBuffer[valueOffset + 14]) >> 1);
        DiagData.KUOZ1 = (sbyte)(((sbyte)replyBuffer[valueOffset + 15]) >> 1);
        DiagData.KUOZ2 = (sbyte)(((sbyte)replyBuffer[valueOffset + 16]) >> 1);
        DiagData.KUOZ3 = (sbyte)(((sbyte)replyBuffer[valueOffset + 17]) >> 1);
        DiagData.KUOZ4 = (sbyte)(((sbyte)replyBuffer[valueOffset + 18]) >> 1);
        DiagData.TWAT = (sbyte)(replyBuffer[valueOffset + 4] - 40);
        DiagData.TAIR = (sbyte)(replyBuffer[valueOffset + 33] - 40);
        DiagData.ALF = (replyBuffer[valueOffset + 5] + 128) / 256f;
        DiagData.AFR = 14.7f * DiagData.ALF;
        DiagData.UFRXX = replyBuffer[valueOffset + 11];
        //diagData.SSM = replyBuffer[valueOffset + 17];
        DiagData.COEFF = (replyBuffer[valueOffset + 12] + 128) / 256f;
        DiagData.DGTC_LEAN = ((replyBuffer[valueOffset + 41] << 8) + replyBuffer[valueOffset + 40]) / 256f;
        DiagData.DGTC_RICH = ((replyBuffer[valueOffset + 39] << 8) + replyBuffer[valueOffset + 38]) / 256f;
        DiagData.Faza = (short)(((sbyte)replyBuffer[valueOffset + 13]) * 3);
        DiagData.INJ = ((replyBuffer[valueOffset + 43] << 8) + replyBuffer[valueOffset + 42]) / 125f;
        DiagData.AIR = ((replyBuffer[valueOffset + 45] << 8) + replyBuffer[valueOffset + 44]) / 10f;
        DiagData.GBC = ((replyBuffer[valueOffset + 47] << 8) + replyBuffer[valueOffset + 46]) / 6f;
        DiagData.SPD = replyBuffer[valueOffset + 31];

        DiagData.ADCKNOCK = replyBuffer[valueOffset + 19] * 0.01953125f; //5f / 256f;
        DiagData.ADCMAF = replyBuffer[valueOffset + 20] * 0.01953125f; //5f / 256f;
        DiagData.ADCTWAT = replyBuffer[valueOffset + 24] * 0.01953125f; //5f / 256f;
        DiagData.ADCTAIR = replyBuffer[valueOffset + 25] * 0.01953125f; //5f / 256f;
        DiagData.ADCUBAT = replyBuffer[valueOffset + 21] * 0.00560546875f; //0.287f * 5f / 256f;
        DiagData.ADCLAM = ((replyBuffer[valueOffset + 35] << 8) + replyBuffer[valueOffset + 34]) * 5f / 0xFFFFf;
        DiagData.ADCTPS = replyBuffer[valueOffset + 23] * 0.01953125f; //5f / 256f;

        var status1 = replyBuffer[valueOffset];
        var status2 = replyBuffer[valueOffset + 1];
        var dkStatus = replyBuffer[valueOffset + 29];

        DiagData.fSTOP = DataHelper.IsBitSet(status1, 0);
        DiagData.fXX = DataHelper.IsBitSet(status1, 1);
        DiagData.fPOW = DataHelper.IsBitSet(status1, 2);
        DiagData.fFUELOFF = DataHelper.IsBitSet(status1, 3);
        DiagData.fDETZONE = DataHelper.IsBitSet(status1, 5);
        DiagData.fDET = DataHelper.IsBitSet(status2, 5);
        DiagData.fADS = DataHelper.IsBitSet(status1, 6);
        DiagData.fLAMREG = DataHelper.IsBitSet(status1, 4);
        DiagData.fLAM = DataHelper.IsBitSet(status2, 7);
        DiagData.fLEARN = DataHelper.IsBitSet(status1, 7);
        DiagData.fLAMRDY = DataHelper.IsBitSet(dkStatus, 0);
        DiagData.fLAMHEAT = DataHelper.IsBitSet(dkStatus, 1);
    }
}