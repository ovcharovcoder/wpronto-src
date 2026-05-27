using System;
using System.Windows.Forms;

namespace WPLaunchGUI;

static class Program
{
    [STAThread]
    static void Main()
    {
        // ВАЖЛИВО: Налаштування HighDPI для чіткого відображення на будь-якому моніторі
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Form1());
    }
}
