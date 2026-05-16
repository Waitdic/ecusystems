using System;
using System.Windows.Forms;
using EcuCommunication;
using EcuCommunication.Protocols;

namespace OpenOLT.GUI;

internal partial class ErrorCodesForm : Form
{
    private static bool _isShow;
    private OltProtocol _oltProtocol;

    private ErrorCodesForm()
    {
        InitializeComponent();
    }

    public static void ShowErrors(IWin32Window owner, OltProtocol oltProtocol)
    {
        if (_isShow) return;

        var ef = new ErrorCodesForm();
        ef.Prepare(oltProtocol);
        ef.Show(owner);

        _isShow = true;
    }

    private void Prepare(OltProtocol oltProtocol)
    {
        _oltProtocol = oltProtocol;
        var errors = oltProtocol.ReadErrors().ToArray();
        var errorStatus = ECUErrorFactory.DecodingErrorStatus(oltProtocol.GetDiagData().ErrorStatus);

        savedError.Items.Clear();
        savedError.Items.AddRange(errors);
        activeError.Items.Clear();
        activeError.Items.AddRange(errorStatus);
    }

    private void ErrorCodesForm_FormClosed(object sender, FormClosedEventArgs e) 
        => _isShow = false;

    private void button1_Click(object sender, EventArgs e)
    {
        _oltProtocol.ClearErrors();
        Prepare(_oltProtocol);
    }

    private void button2_Click(object sender, EventArgs e) 
        => Prepare(_oltProtocol);
}