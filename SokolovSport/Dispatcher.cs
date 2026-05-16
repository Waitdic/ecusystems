using System.Windows.Forms;
using SokolovSport.Dat;
using SokolovSport.EcuComm;
using SokolovSport.Logs;
using SokolovSport.Options;

namespace SokolovSport;

class Dispatcher
{
    private readonly OpenFileDialog _openFileDialog;
    public EcuCommunication Ecu { get; }
    public DatFile DatFile { get; private set; }
    private CalibrLogger _calibrLogger;

    public Dispatcher()
    {
        Ecu = new EcuCommunication(OptionsHelper.Options);
        _openFileDialog = new OpenFileDialog();
    }

    public void OpenDatFileDialog(IWin32Window owner)
    {
        _openFileDialog.InitialDirectory = Application.StartupPath;

        while (true)
        {
            if (_openFileDialog.ShowDialog(owner) != DialogResult.OK || OpenDatFile(_openFileDialog.FileName)) return;
                
            MessageBox.Show(owner, "Dat файл не существует или доступен только для чтения. Загрузка невозможна");
        }
    }

    public bool OpenDatFile(string path)
    {
        if (!DatHelper.TestFile(path)) 
            return false;

        DatFile = new DatFile(path);

        _calibrLogger?.Close();

        _calibrLogger = new CalibrLogger(DatFile, Ecu);

        return true;
    }
}