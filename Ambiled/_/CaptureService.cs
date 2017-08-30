using Ambiled.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace Ambiled.Capturers
{
    [Export(typeof(ICaptureService))]
    public class CaptureService : ICaptureService
    {
        public event EventHandler<FrameEventArgs> Frame;
        public event EventHandler<HandleChangedEventArgs> HandleChanged;

        internal BackgroundWorker worker = new BackgroundWorker();

        internal IntPtr currentHandle;
        internal bool isCapturing;
        internal CaptureMode mode;

        internal int _cols;
        internal int _rows;
        internal int _gridcols;
        internal int _gridrows;


        public CaptureService()
        {
        }

        //int frames = 0;
        //int fps = 0;
        Stopwatch sw = new Stopwatch();


        void worker_DoWork(object sender, DoWorkEventArgs e)
        {

            int frames = 0;
            int fps = 0;

            sw.Start();

            var fpsw = default(Stopwatch);

            if (App.VM.EnableFixedFPS)
            {
                fpsw = new Stopwatch();
                fpsw.Start();
            }

            while (isCapturing)
            {
                if (mode == CaptureMode.WindowsHandle)
                    this.UpdateWindowsHandle();

                if (sw.ElapsedMilliseconds > 1000)
                {
                    if (mode == CaptureMode.AutoAttachHandle && currentHandle == IntPtr.Zero)
                        this.UpdateAutoHandle();
                }

                if (currentHandle == IntPtr.Zero)
                    continue;

                if (_cols == 0 || _rows == 0)
                {
                    Debug.WriteLine("Zero values");
                    continue;
                }
                if (App.VM.EnableFixedFPS && fpsw != null)
                {
                    if (fpsw.ElapsedMilliseconds < 33)
                        continue;

                    fpsw.Restart();
                }

                if (sw.ElapsedMilliseconds > 1000)
                {
                    //if (mode == CaptureMode.AutoAttachHandle && currentHandle == IntPtr.Zero)
                    //    this.UpdateAutoHandle();

                    if (App.VM.EnableCrop)
                    {
                        int cropX = 0;
                        int cropY = 0;

                        CaptureUtils.CaptureHandleCrop(this.currentHandle, App.VM.Is3DSBS, App.VM.Is3DOU,
                            out cropX,
                            out cropY);

                        if (cropX > 0) App.VM.CropX = cropX;
                        if (cropY > 0) App.VM.CropY = cropY;
                    }

                    fps = frames;
                    frames = 0;
                    sw.Restart();
                }


                try
                {
                    //// Capture screenshot
                    //var bitmap = CaptureUtils.CaptureWindowStretchBlt(this.currentHandle,
                    //    this._cols,
                    //     this._rows,
                    //    App.VM.CropX,
                    //    App.VM.CropY,
                    //    App.VM.Is3DSBS,
                    //    App.VM.Is3DOU,
                    //    1,
                    //    this._gridcols,
                    //    this._gridrows
                    //);

                    // Capture screenshot
                    //System.Drawing.Image bitmap2 = null;
                    //var bitmap2 = CaptureUtils.CaptureWindowStretchBlt(this.currentHandle,
                    //    4, // this._cols,
                    //    this._rows,
                    //    App.VM.CropX,
                    //    App.VM.CropY,
                    //    App.VM.Is3DSBS,
                    //    App.VM.Is3DOU
                    //);


                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (isCapturing && this.Frame != null)
                        {
                            //this.Frame(this, new FrameEventArgs(bitmap, null, fps));
                            frames++;
                        }
                    }));
                }
                catch
                {
                    this.HandleLost();

                }
            }
        }

        void HandleLost()
        {
            this.currentHandle = IntPtr.Zero;
            //Messages.AddMessage("Lost handle");
            if (HandleChanged != null)
            {
                HandleChanged(this, new HandleChangedEventArgs(currentHandle, string.Empty));
            }
        }

        void UpdateWindowsHandle()
        {
            var handle = User32.GetForegroundWindow();
            if (currentHandle != handle)
            {
                currentHandle = handle;
                if (HandleChanged != null)
                {
                    var title = User32.GetWindowText(currentHandle);
                    HandleChanged(this, new HandleChangedEventArgs(currentHandle, title));
                }
            }
        }

        void UpdateAutoHandle()
        {
            string autoProcessFilter = @"mpc-hc|vlc|paint";
            var texts = autoProcessFilter.Split('|').ToList();

            var processes = new List<Process>(Process.GetProcesses());
            var autoProcess = (from p in processes
                               let t = texts.Where(t => p.ProcessName.Contains(t)).FirstOrDefault()
                               where !string.IsNullOrEmpty(t)
                               select p).FirstOrDefault();

            if (autoProcess != null)
            {

                this.SetHandle(autoProcess.MainWindowHandle);
                var title = autoProcess.MainWindowTitle;

                //Messages.AddMessage("Auto attaching: " + autoProcess.ProcessName);

                if (HandleChanged != null)
                    HandleChanged(this, new HandleChangedEventArgs(currentHandle, title));
            }
        }

        public void Capture(CaptureMode captureMode)
        {
            this.mode = captureMode;
            //Messages.AddMessage(captureMode.ToString());

            this.Capture();

        }

        public void Capture()
        {
            if (!isCapturing)
            {
                isCapturing = true;
                //Messages.AddMessage("Start Capture");
                
                sw.Start();
            }
        }

        public void Stop()
        {
            //Messages.AddMessage("Stop Capture");
            isCapturing = false;
        }

        public void SetHandle(IntPtr handle)
        {
            this.currentHandle = handle;
        }

        public void SetSize(int width, int height)
        {
            this._cols = width;
            this._rows = height;
        }

        public void SetSize(int width, int height, int x, int y)
        {
            this._cols = width;
            this._rows = height;
            this._gridcols = x;
            this._gridrows = y;
        }

        public void SetMode(CaptureMode captureMode)
        {
            if (captureMode == this.mode)
                return;

            this.mode = captureMode;

            if (captureMode == CaptureMode.AutoAttachHandle)
                currentHandle = IntPtr.Zero;
        }

        public void Start()
        {
            this.Capture();
        }
    }
}
