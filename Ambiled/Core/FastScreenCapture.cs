using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ambiled.Core
{
    public class FastScreenCaptureService
    {
        private static class SafeNativeMethods
        {
            [DllImport("FastScreenCapture.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int Initialize(int outputNum, int width, int height);

            [DllImport("FastScreenCapture.dll")]
            public static extern void Clean();

            [DllImport("FastScreenCapture.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int CaptureScreen(byte[] imageData);
        }

        private static FastScreenCaptureService instance;

        public static FastScreenCaptureService GetInstance()
        {
            return instance = instance ?? new FastScreenCaptureService();
        }

        private FastScreenCaptureService()
        {

        }

        public event EventHandler Captured;
        private CancellationTokenSource captureTaskToken;
        private Task captureTask;
        private byte[] buffer;

        public byte[] GetBuffer => buffer;

        public void Start(int outputNum, int width, int height)
        {
            Stop();

            buffer = null;
            buffer = new byte[width * height * 4];

            SafeNativeMethods.Initialize(outputNum, width, height);

            captureTaskToken = new CancellationTokenSource();
            captureTask = Task.Run(() => Capture(captureTaskToken.Token), captureTaskToken.Token);
        }

        public void Stop(bool wait = true)
        {
            if (captureTask != null)
            {
                captureTaskToken.Cancel();
                if (wait)
                    captureTask.Wait();
                captureTask = null;
            }
        }

        private void Capture(CancellationToken captureTaskToken)
        {
            while (!captureTaskToken.IsCancellationRequested)
            {
                SafeNativeMethods.CaptureScreen(buffer);
                Captured?.Invoke(this, null);
            }
        }
    }
}
