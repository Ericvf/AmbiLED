using Ambiled.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;

namespace Ambiled.Controllers
{
    public interface IControllerService
    {
        IViewModel ViewModel { get; set; }


        event EventHandler<FrameEventArgs> Frame;
        //List<string> GetDevices();
        void SetDevice(string deviceName, int baudRate);

        void Connect();

        void ClearFrame(int size);
        void SendFrame(Image image);

        bool IsEnabled();
        void Disconnect();

        void SendFullFrame(Image image);
    }
}
