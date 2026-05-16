using System.Windows.Forms;
using EcuCommunication.Protocols;
using WidebandLambdaCommunication;

namespace OpenOLT.GUI;

internal partial class DiagGaugePanel : UserControl
{
    private IDiagProtocol _diagProtocol;
    private LambdaAdapter _lambdaAdapter;

    public DiagGaugePanel()
    {
        InitializeComponent();
    }

    public void Prepare(IDiagProtocol diagProtocol, LambdaAdapter lambdaAdapter)
    {
        _diagProtocol = diagProtocol;
        _lambdaAdapter = lambdaAdapter;
        UpdateValue();
    }

    public void UpdateValue()
    {
        if (_diagProtocol == null || _lambdaAdapter == null || !Visible) return;

        var diagData = _diagProtocol.GetDiagData();
        thrGauge.Value = diagData.TRT;
        thrTextBox.Text = diagData.TRT.ToString("0.##");

        rpmGauge.Value = diagData.RPM / 100f;
        rpmTextBox.Text = diagData.RPM.ToString("0.##");

        tempGauge.Value = diagData.TWAT;
        tempTextBox.Text = diagData.TWAT.ToString("0.##");

        afrGauge.Value = diagData.AFR;
        afrTextBox.Text = diagData.AFR.ToString("0.#");
        lc1AfrGauge.Value = _lambdaAdapter.AFR;
        lc1AfrTextBox.Text = _lambdaAdapter.AFR.ToString("0.#");
    }
}