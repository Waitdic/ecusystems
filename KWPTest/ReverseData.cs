using Helper;

namespace KWPTest;

class ReverseData
{
    public static readonly ReverseData instance = new();

    public bool enabled;
    public string requestHeader;
    public string answerHeader;
    public string answerData;

    public byte[] answer;

    public void Prepare()
    {
        answer = DataHelper.StrToByteArray(answerHeader + answerData + "00");
        DataHelper.CalcCRC(answer);
    }
}