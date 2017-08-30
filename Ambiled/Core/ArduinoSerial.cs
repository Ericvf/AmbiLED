using System.Collections.Generic;
using System.IO.Ports;
using System.Windows;

namespace Ambiled.Core
{
    public class ArduinoSerialPort
    {
        protected SerialPort arduinoSerial;

        public bool IsOpen
        {
            get
            {
                return arduinoSerial != null
                    ? arduinoSerial.IsOpen
                    : false;
            }
        }

        public static string[] GetDevices()
        {
            return SerialPort.GetPortNames();
        }
        
        public void Connect(string comPort, int baudRate = 9600)
        {
            arduinoSerial = new SerialPort()
            {
                PortName = comPort,
                BaudRate = baudRate
            };

            try
            {
                arduinoSerial.Open();
            }
            catch {
                MessageBox.Show(
                   $"An error occured opening the SerialPort for {comPort}.", "ArduinoSerialPort error",
                   MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Write(byte[] buffer)
        {
            if (arduinoSerial.IsOpen)
                arduinoSerial.Write(buffer, 0, buffer.Length);
        }

        public void Disconnect()
        {
            if (arduinoSerial != null)
            {
                arduinoSerial.Close();
                arduinoSerial.Dispose();
                arduinoSerial = null;
            }
        }
    }
}
