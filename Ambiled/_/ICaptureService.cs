using Ambiled.Core;
using System;

namespace Ambiled.Capturers
{
    public interface ICaptureService
    {
        event EventHandler<FrameEventArgs> Frame;
        void Stop();
        void Start();

        event EventHandler<HandleChangedEventArgs> HandleChanged;
        void Capture(CaptureMode captureMode);
        void Capture();
        void SetSize(int width, int height);
        void SetSize(int width, int height, int x, int y);
        void SetMode(CaptureMode captureMode);
        void SetHandle(IntPtr spyHandle);
    }
}
