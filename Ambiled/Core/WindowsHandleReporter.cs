using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Ambiled.Core
{
    public interface IWindowsHandleReporter
    {

    }

    [Export(typeof(IWindowsHandleReporter))]
    public class WindowsHandleReporter : IWindowsHandleReporter
    {
        private static class User32
        {
            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            private static extern void GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            public static string GetWindowText(IntPtr hWnd)
            {
                var stringBuilder = new StringBuilder(256);
                GetWindowText(hWnd, stringBuilder, 256);

                return stringBuilder.ToString();
            }
        }

        [Import]
        public ILogger Logger { get; set; }

        [Import]
        public IViewModel ViewModel { get; set; }

        private const int interval = 5000;
        private Timer timer;

        public WindowsHandleReporter()
        {
            timer = new Timer();
            timer.Elapsed += Timer_Elapsed;
            timer.Interval = interval;
            timer.Enabled = true;
        }

        private void Timer_Elapsed(object source, ElapsedEventArgs e)
        {
            if (ViewModel.EnableAuto3d)
            {
                var handle = User32.GetForegroundWindow();

                var currentHandleTitle = User32.GetWindowText(handle);
                if (currentHandleTitle.IndexOf("sbs", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    Logger.Add("Auto detected SBS 3D");
                    ViewModel.Is3DSBS = true;
                }
                else if (currentHandleTitle.IndexOf("hou", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    Logger.Add("Auto detected OU 3D");
                    ViewModel.Is3DOU = true;
                }
                else if (!ViewModel.Is3DOff)
                {
                    Logger.Add("Auto detected 2D");
                    ViewModel.Is3DOff = true;
                }
            }
        }
    }
}
