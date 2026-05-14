namespace KWPTest
{
    public class OltDiagParams
    {
        public byte[] bytes = new byte[95];

        public OltDiagParams()
        {
            //АЦП замка зажигания
            bytes[27] = 0xB0;
        }
    }
}
