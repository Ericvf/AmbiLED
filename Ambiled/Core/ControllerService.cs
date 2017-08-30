using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;

namespace Ambiled.Core
{
    public interface IControllerService
    {
        //void Send(Image image);
        void Send(byte[] image, int width, int height);
        void Start();
        void Stop();
        void Toggle();
        bool IsOpen();
        void RefreshDevices();
    }

    [Export(typeof(IControllerService))]
    public class ControllerService : IControllerService
    {
        [Import]
        public IViewModel ViewModel { get; set; }

        [Import]
        public ILogger Logger { get; set; }

        ArduinoSerialPort arduinoSerial = new ArduinoSerialPort();

        static byte[] frameData = new byte[0];

        static readonly byte[] gamma = new byte[] {
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
          215,218,220,223,225,228,231,233,236,239,241,244,247,249,252,255
        };

        //#region System.Drawing.Image code
        //public void Send(Image image)
        //{
        //    if (ViewModel.IsAmbilight)
        //        SendAmbilight(image);
        //    else if (ViewModel.IsBoxlight)
        //        SendBoxlight(image);
        //}

        //private void SendAmbilight(Image image)
        //{
        //    var bytes = new List<byte>();
        //    int x = 0;
        //    int y = 0;

        //    var capture = new FastBitmap((Bitmap)image);
        //    capture.LockBitmap();

        //    var width = capture.Bitmap.Width;
        //    var height = capture.Bitmap.Height;

        //    x = width - 1;
        //    for (y = 0; y < height; y++)
        //    {
        //        var px = capture.GetPixel(x, height - y - 1);
        //        bytes.AddRange(ConvertBytes(px.R, px.G, px.B));
        //    }

        //    y = 0;
        //    for (x = width - 1; x >= 0; x--)
        //    {
        //        var px = capture.GetPixel(x, y);
        //        bytes.AddRange(ConvertBytes(px.R, px.G, px.B));
        //    }

        //    x = 0;
        //    for (y = 0; y < height; y++)
        //    {
        //        var px = capture.GetPixel(x, y);
        //        bytes.AddRange(ConvertBytes(px.R, px.G, px.B));
        //    }

        //    y = height - 1;
        //    for (x = 0; x < width; x++)
        //    {
        //        var px = capture.GetPixel(x, y);
        //        bytes.AddRange(ConvertBytes(px.R, px.G, px.B));
        //    }

        //    capture.UnlockBitmap();

        //    SendPayload(bytes.ToArray());
        //}

        //private void SendBoxlight(Image image)
        //{
        //    var bytes = new List<byte>();
        //    int x = 0;
        //    int y = 0;

        //    var capture = new FastBitmap((Bitmap)image);
        //    capture.LockBitmap();

        //    var width = capture.Bitmap.Width;
        //    var height = capture.Bitmap.Height;

        //    for (y = 0; y < height; y++)
        //    {
        //        for (x = 0; x < width; x++)
        //        {
        //            var zigzagx = y % 2 == 0 ? x : width - x - 1;
        //            var pixel = capture.GetPixel(zigzagx, height - y - 1);

        //            bytes.AddRange(ConvertBytes(pixel.R, pixel.G, pixel.B));
        //        }
        //    }

        //    capture.UnlockBitmap();

        //    SendPayload(bytes.ToArray());
        //}

        //private void SendPayload(byte[] bytes)
        //{
        //    var payloadHeader = new byte[] { 1, 2, 3 };
        //    var payload = new List<byte>(bytes);
        //    payload.InsertRange(0, payloadHeader);

        //    if (arduinoSerial.IsOpen)
        //        arduinoSerial.Write(payload.ToArray());
        //}


        //byte[] ConvertBytes(byte R, byte G, byte B)
        //{
        //    if (ViewModel.IsBGR)
        //    {
        //        byte r = R;
        //        R = B;
        //        B = r;
        //    }
        //    else if (ViewModel.IsRBG)
        //    {
        //        byte b = B;
        //        B = G;
        //        G = b;
        //    }

        //    if (ViewModel.Is2bit)
        //        return this.ConvertBytes2bit(R, G, B);
        //    if (ViewModel.Is5bit)
        //        return this.ConvertBytes5bit(R, G, B);
        //    else if (ViewModel.Is8bit)
        //        return this.ConvertBytes8bit(R, G, B);
        //    else
        //        return new byte[0];
        //}

        //byte[] ConvertBytes2bit(byte R, byte G, byte B)
        //{
        //    byte rb = (byte)(((double)R / 255) * 3);
        //    byte gb = (byte)(((double)G / 255) * 3);
        //    byte bb = (byte)(((double)B / 255) * 3);

        //    //if (App.VM.EnableGamma)
        //    //{
        //    //    var fr = Math.Pow((float)R / 255.0, App.VM.GammeValue);
        //    //    var fg = Math.Pow((float)G / 255.0, App.VM.GammeValue);
        //    //    var fb = Math.Pow((float)B / 255.0, App.VM.GammeValue);

        //    //    fr = (byte)(fr * 255.0);
        //    //    fg = (byte)(fg * 255.0);
        //    //    fb = (byte)(fb * 255.0);
        //    //}

        //    int command = ((int)rb & 3) << 4 | ((int)gb & 3) << 2 | ((int)bb & 3);
        //    return new[] { (byte)command };
        //}

        //byte[] ConvertBytes5bit(byte R, byte G, byte B)
        //{
        //    byte rb = (byte)(((double)gamma[R] / 255) * 31);
        //    byte gb = (byte)(((double)gamma[G] / 255) * 31);
        //    byte bb = (byte)(((double)gamma[B] / 255) * 31);

        //    if (ViewModel.EnableGamma)
        //    {
        //        var fr = Math.Pow((float)R / 255.0, 1.6);
        //        var fg = Math.Pow((float)G / 255.0, 1.6);
        //        var fb = Math.Pow((float)B / 255.0, 1.6);

        //        fr = (byte)(fr * 255.0);
        //        fg = (byte)(fg * 255.0);
        //        fb = (byte)(fb * 255.0);
        //    }

        //    int command = ((int)rb & 0x1F) << 10 | ((int)gb & 0x1F) << 5 | ((int)bb & 0x1F);
        //    return BitConverter.GetBytes((short)command);
        //}

        //byte[] ConvertBytes8bit(byte R, byte G, byte B)
        //{
        //    //return new[] { R, G, B };

        //    if (ViewModel.EnableGamma)
        //    {
        //        //float gammaValue = App.VM.GammeValue;

        //        var fr = Math.Pow((float)R / 255.0, ViewModel.RChannel + 1f);
        //        var fg = Math.Pow((float)G / 255.0, ViewModel.GChannel + 1f);
        //        var fb = Math.Pow((float)B / 255.0, ViewModel.BChannel + 1f);

        //        return new[] {
        //            (byte)(fr * 255.0),
        //            (byte)(fg * 255.0),
        //            (byte)(fb * 255.0)
        //        };
        //    }
        //    else
        //    {
        //        return new[] { gamma[R], gamma[G], gamma[B] };
        //    }
        //}
        //#endregion

        public void Start()
        {
            Logger.Add($"Establishing connection with device on {ViewModel.ComDevice} @ {ViewModel.BaudRate}...");
            arduinoSerial.Connect(ViewModel.ComDevice, ViewModel.BaudRate);
        }

        public void Stop()
        {
            Logger.Add("Disconnect device");
            arduinoSerial.Disconnect();
        }

        public void Toggle()
        {
            if (arduinoSerial.IsOpen)
                Stop();
            else
                Start();
        }

        public bool IsOpen()
        {
            return arduinoSerial.IsOpen;
        }

        public void RefreshDevices()
        {
            Logger.Add("Loading devices");
            ViewModel.ComDevices = new ObservableCollection<string>(ArduinoSerialPort.GetDevices());
        }

        public void Send(byte[] bytes, int width, int height)
        {
            if (ViewModel.IsAmbilight)
                ConvertBufferAmbilight(bytes, width, height);
            else if (ViewModel.IsBoxlight)
                ConvertBufferBoxLight(bytes, width, height);

            if (arduinoSerial.IsOpen)
                arduinoSerial.Write(frameData);
        }

        private byte[] ConvertBufferAmbilight(byte[] bytes, int width, int height)
        {
            int size = (width + height) * 2 * 3 + 3;
            int maxx = width - 1;
            int maxy = height - 1;
            int p = 0;

            if (frameData.Length != size)
                Array.Resize(ref frameData, size);

            frameData[p++] = 1;
            frameData[p++] = 2;
            frameData[p++] = 3;

            for (int y = maxy; y >= 0; y--)
            {
                var offset = (maxx + y * width) * 4;
                frameData[p++] = gamma[bytes[offset + 0]];
                frameData[p++] = gamma[bytes[offset + 1]];
                frameData[p++] = gamma[bytes[offset + 2]];
            }

            for (int x = maxx; x >= 0; x--)
            {
                var offset = (x + width) * 4;
                frameData[p++] = gamma[bytes[offset + 0]];
                frameData[p++] = gamma[bytes[offset + 1]];
                frameData[p++] = gamma[bytes[offset + 2]];
            }

            for (int y = 0; y <= maxy; y++)
            {
                var offset = (y * width) * 4;
                frameData[p++] = gamma[bytes[offset + 0]];
                frameData[p++] = gamma[bytes[offset + 1]];
                frameData[p++] = gamma[bytes[offset + 2]];
            }

            for (int x = 0; x <= maxx; x++)
            {
                var offset = (x + maxy * width) * 4;
                frameData[p++] = gamma[bytes[offset + 0]];
                frameData[p++] = gamma[bytes[offset + 1]];
                frameData[p++] = gamma[bytes[offset + 2]];
            }

            return frameData;
        }

        private byte[] ConvertBufferBoxLight(byte[] bytes, int width, int height)
        {
            int size = width * height * 3 + 3;
            int maxx = width - 1;
            int maxy = height - 1;
            int p = 0;

            if (frameData.Length != size)
                Array.Resize(ref frameData, size);

            frameData[p++] = 1;
            frameData[p++] = 2;
            frameData[p++] = 3;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var zigzagx = y % 2 == 0 ? x : maxx - x;
                    var offset = (zigzagx + (maxy - y) * width) * 4;
                    frameData[p++] = gamma[bytes[offset + 2]];
                    frameData[p++] = gamma[bytes[offset + 1]];
                    frameData[p++] = gamma[bytes[offset + 0]];
                }
            }

            return frameData;
        }
    }
}
