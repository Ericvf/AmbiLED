using System;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace Ambiled.Core
{
    public class FrameEventArgs : EventArgs
    {
        public Bitmap Image { get; set; }
        public WriteableBitmap Image2 { get; set; }
        public byte[] Buffer { get; set; }
        public int FPS { get; set; }

        public FrameEventArgs(Bitmap image, int fps)
        {
            this.Image = image;
            this.FPS = fps;
        }

        public FrameEventArgs(Bitmap image, WriteableBitmap image2, int fps)
        {
            this.Image = image;
            this.Image2 = image2;
            this.FPS = fps;
        }
    }
}
