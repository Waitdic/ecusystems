using System;
using System.Windows.Forms;

namespace KWPTest;

public partial class OltDiagForm : Form
{
    private readonly OltDiagParams _diagParams;

    public OltDiagForm(OltDiagParams diagParams)
    {
        _diagParams = diagParams;
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
        _diagParams.bytes[0] = byte0.Value;
        _diagParams.bytes[1] = byte1.Value;

        foreach (Control control in Controls)
        {
            if (control.Tag == null) continue;

            switch (control)
            {
                case IByteSetter setter:
                {
                    var index = Convert.ToInt32(control.Tag);
                    _diagParams.bytes[index] = setter.Value;
                    break;
                }
                case IWordSetter setter:
                {
                    var index = Convert.ToInt32(control.Tag);
                    _diagParams.bytes[index] = setter.Byte1;
                    _diagParams.bytes[index + 1] = setter.Byte2;
                    break;
                }
            }
        }
    }

    private void bytes_OnValueChange(object sender, EventArgs e)
    {
        ParamsApply();
    }

    private void word_OnValueChange(object sender, EventArgs e)
    {
        ParamsApply();
    }
}