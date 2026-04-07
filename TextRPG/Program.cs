// ────────────────────────────────────────────────────────────
// TEXT RPG — Program Entry Point
// Purpose: Launches the WinForms application
// ────────────────────────────────────────────────────────────
using System;
using System.Windows.Forms;

namespace TextRPG
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
