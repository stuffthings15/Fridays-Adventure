using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Global exception capture for UI-thread exceptions.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
                DebugLogger.LogError("Application.ThreadException", e.Exception);

            // Global exception capture for non-UI thread exceptions.
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                DebugLogger.LogError("AppDomain.UnhandledException", e.ExceptionObject as Exception);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
