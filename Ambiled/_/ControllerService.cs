using Ambiled.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;

namespace Ambiled.Controllers
{
    public static class LinqExtensions
    {
        /// <summary>
        /// Break a list of items into chunks of a specific size
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunksize)
        {
            while (source.Any())
            {
                yield return source.Take(chunksize);
                source = source.Skip(chunksize);
            }
        }
    }

    public class ControllerService : IControllerService
    {
        [Import]
        public IViewModel ViewModel { get; set; }

        ArduinoSerial arduinoSerial = new ArduinoSerial();
        BackgroundWorker worker = new BackgroundWorker();
        byte[] arduinoFrame;

        public event EventHandler<FrameEventArgs> Frame;

        internal string currentDevice;
        internal int baudRate;

        public ControllerService()
        {
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (this.Frame != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.Frame(this, new FrameEventArgs(null, 0));
                }));
            }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var sw = new Stopwatch();
            int frames = 0;
            int fps = 0;

            sw.Start();
            while (arduinoSerial.IsOpen)
            {
                if (arduinoFrame != null)
                {
                    //************ ambilight
                    //arduinoSerial.Write(new byte[] { 1, 2, 3 });

                    //foreach (var chunk in arduinoFrame.Chunk(64))
                    //{
                    //    arduinoSerial.Write(chunk.ToArray());
                    //};

                    var s = new byte[] { 1, 2, 3 };
                    var payload = new List<byte>();//
                    payload.AddRange(s);
                    payload.AddRange(arduinoFrame);
                    arduinoSerial.Write(payload.ToArray());

                    frames++;
                    arduinoFrame = null;
                    //************
                }

                if (sw.ElapsedMilliseconds > 1000)
                {
                    fps = frames;
                    frames = 0;
                    sw.Restart();

                    if (this.Frame != null)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.Frame(this, new FrameEventArgs(null, fps));
                        }));
                    }
                }
            }
        }

        //public List<string> GetDevices()
        //{
        //    Messages.AddMessage("Loading SerialPort devices");

        //    string[] ports = SerialPort.GetPortNames();
        //    return new List<string>(ports);
        //}

        public void SetDevice(string deviceName, int baudRate)
        {
            this.currentDevice = deviceName;
            this.baudRate = baudRate;
        }

        public void Connect()
        {
            try
            {
                if (currentDevice == null)
                    throw new ArgumentNullException("Controller device not selected");

                //arduinoSerial.Connect(this.currentDevice, this.baudRate);
                Thread.Sleep(100);

                //arduinoSerial.Write(new byte[] { 222,222,222,222,222});
              //  worker.RunWorkerAsync();
            }
            catch (Exception)
            {
                //Messages.AddMessage(ex.Message);
                throw;
            }
        }

        public void Disconnect()
        {
            this.arduinoSerial.Disconnect();
        }
        byte[] gamma = {
    0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
    0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  1,  1,  1,  1,
    1,  1,  1,  1,  1,  1,  1,  1,  1,  2,  2,  2,  2,  2,  2,  2,
    2,  3,  3,  3,  3,  3,  3,  3,  4,  4,  4,  4,  4,  5,  5,  5,
    5,  6,  6,  6,  6,  7,  7,  7,  7,  8,  8,  8,  9,  9,  9, 10,
   10, 10, 11, 11, 11, 12, 12, 13, 13, 13, 14, 14, 15, 15, 16, 16,
   17, 17, 18, 18, 19, 19, 20, 20, 21, 21, 22, 22, 23, 24, 24, 25,
   25, 26, 27, 27, 28, 29, 29, 30, 31, 32, 32, 33, 34, 35, 35, 36,
   37, 38, 39, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 50,
   51, 52, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 66, 67, 68,
   69, 70, 72, 73, 74, 75, 77, 78, 79, 81, 82, 83, 85, 86, 87, 89,
   90, 92, 93, 95, 96, 98, 99,101,102,104,105,107,109,110,112,114,
  115,117,119,120,122,124,126,127,129,131,133,135,137,138,140,142,
  144,146,148,150,152,154,156,158,160,162,164,167,169,171,173,175,
  177,180,182,184,186,189,191,193,196,198,200,203,205,208,210,213,
  215,218,220,223,225,228,231,233,236,239,241,244,247,249,252,255 };

        byte[] ConvertBytes(byte R, byte G, byte B)
        {
            if (App.VM.IsBGR)
            {
                byte r = R;
                R = B;
                B = r;
            }
            else if (App.VM.IsRBG)
            {
                byte b = B;
                B = G;
                G = b;
            }

            if (App.VM.Is2bit)
                return this.ConvertBytes2bit(R, G, B);
            if (App.VM.Is5bit)
                return this.ConvertBytes5bit(R, G, B);
            else if (App.VM.Is8bit)
                return this.ConvertBytes8bit(R, G, B);
            else
                return new byte[0];
        }

        byte[] ConvertBytes2bit(byte R, byte G, byte B)
        {
            byte rb = (byte)(((double)R / 255) * 3);
            byte gb = (byte)(((double)G / 255) * 3);
            byte bb = (byte)(((double)B / 255) * 3);

            //if (App.VM.EnableGamma)
            //{
            //    var fr = Math.Pow((float)R / 255.0, App.VM.GammeValue);
            //    var fg = Math.Pow((float)G / 255.0, App.VM.GammeValue);
            //    var fb = Math.Pow((float)B / 255.0, App.VM.GammeValue);

            //    fr = (byte)(fr * 255.0);
            //    fg = (byte)(fg * 255.0);
            //    fb = (byte)(fb * 255.0);
            //}
      
            int command = ((int)rb & 3) << 4 | ((int)gb & 3) << 2 | ((int)bb & 3);
            return new[] {(byte)command};
        }

        byte[] ConvertBytes5bit(byte R, byte G, byte B)
        {
            byte rb = (byte)(((double)gamma[R] / 255) * 31);
            byte gb = (byte)(((double)gamma[G] / 255) * 31);
            byte bb = (byte)(((double)gamma[B] / 255) * 31);

            if (App.VM.EnableGamma)
            {
                var fr = Math.Pow((float)R / 255.0, 1.6);
                var fg = Math.Pow((float)G / 255.0, 1.6);
                var fb = Math.Pow((float)B / 255.0, 1.6);

                fr = (byte)(fr * 255.0);
                fg = (byte)(fg * 255.0);
                fb = (byte)(fb * 255.0);
            }
            
            int command = ((int)rb & 0x1F) << 10 | ((int)gb & 0x1F) << 5 | ((int)bb & 0x1F);
            return BitConverter.GetBytes((short)command);
        }

        byte[] ConvertBytes8bit(byte R, byte G, byte B)
        {
            //return new[] { R, G, B };

            if (App.VM.EnableGamma)
            {
                //float gammaValue = App.VM.GammeValue;

                var fr = Math.Pow((float)R / 255.0, App.VM.RChannel + 1f);
                var fg = Math.Pow((float)G / 255.0, App.VM.GChannel + 1f);
                var fb = Math.Pow((float)B / 255.0, App.VM.BChannel + 1f);

                return new[] { 
                    (byte)(fr * 255.0),
                    (byte)(fg * 255.0),
                    (byte)(fb * 255.0)
                };
            }
            else
            {
                return new[] { gamma[R], gamma[G], gamma[B] };
            }
        }

        public void SendFullFrame(System.Drawing.Image image)
        {

            var bytes = new List<byte>();
            int x = 0;
            int y = 0;

            var capture = new FastBitmap((System.Drawing.Bitmap)image);
            capture.LockBitmap();

            var width = capture.Bitmap.Width;
            var height = capture.Bitmap.Height;
            
            for (y = 0; y < height; y++)
			{
                for (x = 0; x < width; x++)
			    {
                    int dy = x % 2 == 0
                        ? height - y - 1
                        : y;

                    int dx = y % 2 == 0
                        ? x
                        : width - x - 1;

                    var px = capture.GetPixel(dx, height - y - 1);

                    bytes.AddRange(ConvertBytes(px.R, px.G, px.B));
			    }
			}

            capture.UnlockBitmap();
            this.arduinoFrame = bytes.ToArray();
            var s = new byte[] { 1, 2, 3 };
            var payload = new List<byte>();//
            payload.AddRange(s);
            payload.AddRange(arduinoFrame);

            if (arduinoSerial.IsOpen)
                arduinoSerial.Write(payload.ToArray());

        }

        public void SendFrame(System.Drawing.Image bitmap)
        {
            var bytes = new List<byte>();
            int x = 0;
            int y = 0;

            var capture = new FastBitmap((System.Drawing.Bitmap)bitmap);
            capture.LockBitmap();

            var width = capture.Bitmap.Width;
            var height = capture.Bitmap.Height;


            x = width - 1;
            for (y = 0; y < height; y++)
            {
                var px = capture.GetPixel(x, height - y - 1);
                bytes.AddRange(ConvertBytes(px.R, px.G, px.B));
            }

            //y = 0;
            //for (x = width - 2; x > 0; x--)
            //{
            //    var px = capture.GetPixel(x, y);
            //    bytes.AddRange(ConvertBytes(px.R, px.G, px.B));
            //}

            y = 0;
            for (x = width - 1; x >= 0; x--)
            {
                var px = capture.GetPixel(x, y);
                bytes.AddRange(ConvertBytes(px.R, px.G, px.B));
            }

            x = 0;
            for (y = 0; y < height; y++)
            {
                var px = capture.GetPixel(x, y);
                bytes.AddRange(ConvertBytes(px.R, px.G, px.B));
            }

            y = height - 1;
            for (x = 0; x < width; x++)
            {
                var px = capture.GetPixel(x, y);
                bytes.AddRange(ConvertBytes(px.R, px.G, px.B));
            }

            capture.UnlockBitmap();

            this.arduinoFrame = bytes.ToArray();
        }

        public bool IsEnabled()
        {
            return false;// arduinoSerial.IsOpen;
        }

        public void ClearFrame(int size)
        {
            this.arduinoFrame = Enumerable.Repeat((byte)0, size).ToArray();
        }

        //FastScreenCaptureService captureService = FastScreenCaptureService.GetInstance();
        //public void SendRaw(int width, int height)
        //{
        //    var frameData = new List<byte>();

        //    // header
        //    frameData.AddRange(new byte[] { 1, 2, 3 });

        //    for (int y = 0; y < height; y++)
        //    {
        //        for (int x = 0; x < width; x++)
        //        {
        //            var zigzagx = y % 2 == 0 ? x : width - x - 1;
        //            var offset = (zigzagx + (height - y - 1) * height) * 4;

        //            frameData.AddRange(new[] {
        //                captureService.GetBuffer[offset + 2],
        //                captureService.GetBuffer[offset + 1],
        //                captureService.GetBuffer[offset]
        //            });
        //        }
        //    }

        //    arduinoSerial.Write(frameData.ToArray());
        //}
    }
}
