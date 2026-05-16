using System;
using Helper;

namespace EcuCommunication.Protocols.Requests;

internal class J7esOltDiagDataRequest : OltDiagV1DataRequest
{
    private readonly J7esDiagData _j7EsDiagData;
    
    public J7esOltDiagDataRequest(): base("8210F1210DB1")
    {
        _j7EsDiagData = new J7esDiagData();
        diagData = _j7EsDiagData;
    }

    protected override void ParseValues()
    {
        base.ParseValues();

        var index = replyValueOffset + 52;
            
        _j7EsDiagData.PRESS_RT = replyBuffer[index++];
        var tcharge = replyBuffer[index++];
        _j7EsDiagData.TchargeCoeff = replyBuffer[index++] / 256f;
        _j7EsDiagData.Tcharge = (sbyte) (tcharge - 40*_j7EsDiagData.TchargeCoeff);
        _j7EsDiagData.DUOZ = ((sbyte)replyBuffer[index++]) / 2f;
        _j7EsDiagData.LaunchFuelCutOff = (short)(replyBuffer[index++] * 40);
        _j7EsDiagData.KGTC = replyBuffer[index++] / 128f;
        _j7EsDiagData.KnockFlags = replyBuffer[index++];
        _j7EsDiagData.MisFireFlags = replyBuffer[index++];
        _j7EsDiagData.KGBC_DAD = replyBuffer[index++] / 128f;
        _j7EsDiagData.UOZ_TCORR = ((sbyte)replyBuffer[index++]) / 2f;
        _j7EsDiagData.KBARR_GTC = replyBuffer[index++] / 128f;
        _j7EsDiagData.JUFRXX = replyBuffer[index++] * 10;
        _j7EsDiagData.JFRXX1 = replyBuffer[index++] * 10;
        _j7EsDiagData.JFRXX2 = replyBuffer[index++] * 10;
        _j7EsDiagData.KINJ_AIRFREE = replyBuffer[index++] / 256f;
        _j7EsDiagData.DUOZ_REGXX = ((sbyte)replyBuffer[index++]) / 2f;
        _j7EsDiagData.DERR_RPM = (short) (((sbyte)replyBuffer[index++]) * 10);
        _j7EsDiagData.DIUOZ = ((sbyte)replyBuffer[index++]) / 2f;
        _j7EsDiagData.DELAY_FUEL_CUTOFF = replyBuffer[index++];
        _j7EsDiagData.TLE_PIN_0_7 = replyBuffer[index++];
        _j7EsDiagData.TLE_PIN_8_15 = replyBuffer[index++];
        _j7EsDiagData.TURBO_DYNAMICS = (byte) (((sbyte)replyBuffer[index++]) * 100f / 255f);
        _j7EsDiagData.WGDC = (byte) (replyBuffer[index++] * 100f / 255f);

        _j7EsDiagData.TWAT_RT = replyBuffer[index++];
        _j7EsDiagData.RPM_RT = replyBuffer[index++];            
        _j7EsDiagData.RPM_THR_RT = replyBuffer[index++];

        int rpm_rt_16;
        _j7EsDiagData.THR_RT_16 = (byte) Math.DivRem(_j7EsDiagData.RPM_THR_RT, 16, out rpm_rt_16);
        _j7EsDiagData.RPM_RT_16 = (byte) rpm_rt_16;
        //j7esDiagData.RPM_RT_16 = (byte)(DataHelper.Swap((byte)(j7esDiagData.RPM_RT + 8)) & 0xF);

        _j7EsDiagData.StartFlags = replyBuffer[index++];
        //if (DataHelper.IsBitSet(j7esDiagData.StartFlags, 0))
        //    j7esDiagData.FUSE *= 2f;
        _j7EsDiagData.DELTA_RPM_XX = ((sbyte)replyBuffer[index++]) * 10;
        _j7EsDiagData.PXX_ZONE = replyBuffer[index++];
        _j7EsDiagData.UGB_RXX = replyBuffer[index++] / 5f;
        _j7EsDiagData.DUOZ_LAUNCH = replyBuffer[index++]*6;
        _j7EsDiagData.GBC_RT = replyBuffer[index++];
        _j7EsDiagData.GBC_RT_16 = (byte)(DataHelper.Swap((byte)(_j7EsDiagData.RPM_RT + 8)) & 0xF);
        _j7EsDiagData.Knock = (ushort)((replyBuffer[index + 1] << 8) + replyBuffer[index]);
        index += 2;

        _j7EsDiagData.FchargeGBC = (ushort)((replyBuffer[index + 1] << 8) + replyBuffer[index]);
        index += 2;
        _j7EsDiagData.FchargeGBCFin = (ushort) ((replyBuffer[index + 1] << 8) + replyBuffer[index]);
        index += 2;
        _j7EsDiagData.Press = ((replyBuffer[index + 1] << 8) + replyBuffer[index]) / 100f;
        index += 2;            
        _j7EsDiagData.DGTC_DadRich = ((replyBuffer[index + 1] << 8) + replyBuffer[index]) / 256f;
        index += 2;
        _j7EsDiagData.TARGET_BOOST = ((replyBuffer[index + 1] << 8) + replyBuffer[index]) / 100f;            
    }
}