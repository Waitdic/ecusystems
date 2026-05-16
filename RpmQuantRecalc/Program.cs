using System;
using System.Windows.Forms;

namespace RpmQuantRecalc;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        
#pragma warning disable CS0436 // Type conflicts with imported type
        Application.Run(new MainForm());
#pragma warning restore CS0436 // Type conflicts with imported type
    }
}

