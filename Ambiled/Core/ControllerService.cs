using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;

namespace Ambiled.Core
{
    public interface IControllerService
    {
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

            if (string.IsNullOrEmpty(ViewModel.ComDevice) || !ViewModel.ComDevices.Contains(ViewModel.ComDevice))
                ViewModel.ComDevice = ViewModel.ComDevices
                    .OrderBy(x => x)
                    .LastOrDefault();
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
