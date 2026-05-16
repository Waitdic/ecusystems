using System;
using Helper;

namespace EcuCommunication.Protocols.Requests;

internal sealed class Rus83DiagDataRequest : JRequest, IDiagDataRequest
{
    public DiagData DiagData { get; }

    public Rus83DiagDataRequest() : base("8210F12101A5")
    {
        testCrc = false;
        DiagData = new DiagData();
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
        var valueOffset = replyValueOffset + 1;
        if ((valueOffset + 26) > replyBuffer.Length) return;

        DiagData.TRT = replyBuffer[valueOffset + 10];
        DiagData.RPM = DiagData.RPM40 = (ushort)(replyBuffer[valueOffset + 11] * 40);
        DiagData.RPM_XX = (ushort)(replyBuffer[valueOffset + 12] * 10);
        DiagData.UOZ = (sbyte)(((sbyte)replyBuffer[valueOffset + 16]) >> 1);
        DiagData.TWAT = (sbyte)(replyBuffer[valueOffset + 8] - 40);
        //diagData.ALF = (replyBuffer[valueOffset + 11] + 128) / 256f;
        //diagData.AFR = 14.7f * diagData.ALF;
        DiagData.UFRXX = replyBuffer[valueOffset + 13];
        DiagData.SSM = replyBuffer[valueOffset + 14];
        DiagData.COEFF = (replyBuffer[valueOffset + 15] + 128) / 256f;
        DiagData.INJ = ((replyBuffer[valueOffset + 21] << 8) + replyBuffer[valueOffset + 20]) / 125f;
        DiagData.AIR = ((replyBuffer[valueOffset + 23] << 8) + replyBuffer[valueOffset + 22]) / 10f;
        DiagData.GBC = ((replyBuffer[valueOffset + 25] << 8) + replyBuffer[valueOffset + 24]) / 6f;
        DiagData.SPD = replyBuffer[valueOffset + 17];

        var status1 = replyBuffer[valueOffset + 2];
        var status2 = replyBuffer[valueOffset + 3];
        //var dkStatus = replyBuffer[valueOffset + 23];

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
        //diagData.fLAMRDY = DataHelper.IsBitSet(dkStatus, 0);
        //diagData.fLAMHEAT = DataHelper.IsBitSet(dkStatus, 1);

        DiagData.ErrorStatus = (uint)(replyBuffer[valueOffset + 4] << 32 + replyBuffer[valueOffset + 5] <<
                                      16 + replyBuffer[valueOffset + 6] << 8 + replyBuffer[valueOffset + 7]);
    }
}