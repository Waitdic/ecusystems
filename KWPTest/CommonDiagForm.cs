using System;
using System.Windows.Forms;

namespace KWPTest;

public partial class CommonDiagForm : Form
{
    private readonly CommonDiagParams _commonDiagParams;

    public CommonDiagForm(CommonDiagParams commonDiagParams)
    {
        _commonDiagParams = commonDiagParams;
        InitializeComponent();
    }

    private void OltDiagForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing) return;
        e.Cancel = true;
        Hide();
    }

    private void ParamsApply()
    {
        _commonDiagParams.bytes[0] = byte0.Value;
        _commonDiagParams.bytes[1] = byte1.Value;
        _commonDiagParams.bytes[2] = byte2.Value;
        _commonDiagParams.bytes[3] = byte3.Value;
        _commonDiagParams.bytes[4] = byte4.Value;
        _commonDiagParams.bytes[5] = byte5.Value;
        _commonDiagParams.bytes[6] = byte6.Value;
        _commonDiagParams.bytes[7] = byte7.Value;

        foreach (Control control in Controls)
        {
            if (control.Tag == null) continue;

            switch (control)
            {
                case IByteSetter setter:
                {
                    var index = Convert.ToInt32(control.Tag);
                    _commonDiagParams.bytes[index] = setter.Value;
                    break;
                }
                case IWordSetter setter:
                {
                    var index = Convert.ToInt32(control.Tag);
                    _commonDiagParams.bytes[index] = setter.Byte1;
                    _commonDiagParams.bytes[index + 1] = setter.Byte2;
                    break;
                }
            }
        }
    }

    private void byte_OnValueChange(object sender, EventArgs e) => ParamsApply();
}