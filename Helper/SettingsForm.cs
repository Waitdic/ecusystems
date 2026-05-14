using System.Windows.Forms;

namespace Helper
{
    internal partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
        }

        public void Prepare(object settings)
        {
            propertyGrid.SelectedObject = settings;
        }
    }
}
