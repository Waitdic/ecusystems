using System.Windows.Forms;
using EcuCommunication.Protocols;
using OpenOLT.DataValueInfo;

namespace OpenOLT.GUI;

internal partial class DiagValueControl : UserControl
{
    private ValueInfo _valueInfo;

    public DiagValueControl()
    {
        InitializeComponent();
    }

    public void Init(ValueInfo valueInfo)
    {
        _valueInfo = valueInfo;
        Name = valueInfo.Name;
        Tag = valueInfo;
        nameLabel.Text = valueInfo.Title;
        valueLabel.Text = "0";
    }

    public void SetValue(DiagData data) 
        => valueLabel.Text = data.GetValue(_valueInfo.Name);
}