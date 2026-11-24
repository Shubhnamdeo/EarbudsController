using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace EarbudsController
{
    static class Program
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint RegisterWindowMessage(string lpString);

        public const string SHOW_MESSAGE_NAME = "EarbudsController_Show_Window_Broadcast_v3";
        public static uint WM_SHOW_WINDOW = 0;

        [STAThread]
        static void Main()
        {
            WM_SHOW_WINDOW = RegisterWindowMessage(SHOW_MESSAGE_NAME);
            // Kill any old zombie/lingering EarbudsController instances so fresh code always runs!
            try
            {
                int currentId = Process.GetCurrentProcess().Id;
                foreach (var proc in Process.GetProcessesByName("EarbudsController"))
                {
                    try
                    {
                        if (proc.Id != currentId)
                        {
                            proc.Kill();
                            proc.WaitForExit(1000);
                        }
                    }
                    catch { }
                }
            }
            catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Application.Run(new TrayApplicationContext());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting Earbuds Controller: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
