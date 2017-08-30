using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;

namespace Ambiled.Core
{
    public interface ICaptureService
    {
        void Reset();
        void Close();
    }

    [Export(typeof(ICaptureService))]
    public class CaptureService : ICaptureService
    {
        [Import]
        public IControllerService ControllerService { get; set; }

        [Import]
        private IViewModel ViewModel { get; set; }

        [Import]
        private ILogger Logger { get; set; }

        private FastScreenCaptureService captureService;
        private WriteableBitmap writeableBitmap;

        private const int frameTime = 1000 / 60;
        private Stopwatch frameTimer;

        private Stopwatch fpsTimer;
        private int fpsTotal = 0;
        private int fps = 0;

        static byte[] postProcessedBackBuffer = new byte[0];
        static byte[] postProcessedBuffer = new byte[0];
        int width = 16;
        int height = 16;
        int screen = 0;

        Bitmap previousBitmap;

        [ImportingConstructor]
        public CaptureService(IViewModel ViewModel)
        {
            captureService = FastScreenCaptureService.GetInstance();

            screen = ViewModel.MonitorTwo ? 1 : 0;
            width = ViewModel.Columns;
            height = ViewModel.Rows;

            writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            captureService.Captured += Service_Captured;
            captureService.Start(screen, width, height);

            frameTimer = new Stopwatch();
            fpsTimer = new Stopwatch();
            fpsTimer.Start();
        }

        private void Service_Captured(object sender, EventArgs e)
        {
            bool useFrameTimer = ViewModel.EnableFixedFPS;
            if (useFrameTimer)
                frameTimer.Restart();

            if (ViewModel.ShowPreview)
            {
                Task.Run(() =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        writeableBitmap.WritePixels(new Int32Rect(0, 0, width, height),
                            captureService.GetBuffer, width * 4, 0);

                        ViewModel.Image = writeableBitmap;
                    });
                });
            }

            if (fpsTimer.ElapsedMilliseconds > 1000)
            {
                ViewModel.FPS = fpsTotal = fps;
                fpsTimer.Restart();
                fps = 0;
            }
            else fps++;

            if (!ControllerService.IsOpen())
            {
                Thread.Sleep(frameTime);
                return;
            }

            if (ViewModel.EnablePostprocessing)
            {
                //if (!ViewModel.EnableSmoothing)
                //{
                PostProcessImage();
                ControllerService.Send(postProcessedBuffer, width, height);
                //}
                //else
                //{
                //    var gcHandle = GCHandle.Alloc(captureService.GetBuffer, GCHandleType.Pinned);
                //    var bitmap = new Bitmap(width, height, width * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, gcHandle.AddrOfPinnedObject());

                //    if (ViewModel.EnablePostprocessing)
                //        bitmap = PostProcessImage(bitmap);

                //    ControllerService.Send(bitmap);
                //}
            }
            else
                ControllerService.Send(captureService.GetBuffer, width, height);

            if (useFrameTimer)
            {
                frameTimer.Stop();
                Thread.Sleep(Math.Max(0, frameTime - (int)frameTimer.ElapsedMilliseconds));
            }
        }

        public void Close()
        {
            captureService.Stop();
        }

        public void Reset()
        {
            screen = ViewModel.MonitorTwo ? 1 : 0;
            width = ViewModel.Columns;
            height = ViewModel.Rows;

            Logger.Add($"Update setup: x={width} y={height}");

            captureService.Stop();

            writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            fpsTimer = new Stopwatch();
            fpsTimer.Start();

            captureService.Start(screen, width, height);
        }

        //private Bitmap PostProcessImage(Bitmap bitmap)
        //{
        //    var prevFastBitmap = default(FastBitmap);
        //    if (ViewModel.EnableSmoothing && previousBitmap != null)
        //    {
        //        prevFastBitmap = new FastBitmap(previousBitmap);
        //        prevFastBitmap.LockBitmap();
        //    }

        //    var capture = new FastBitmap(bitmap);
        //    capture.LockBitmap();

        //    int x = 0;
        //    int y = 0;

        //    for (y = 0; y < capture.Bitmap.Height; y++)
        //    {
        //        for (x = 0; x < capture.Bitmap.Width; x++)
        //        {
        //            var capturePixel = capture.GetPixel(x, bitmap.Height - y - 1);

        //            if (ViewModel.EnableSmoothing && prevFastBitmap != null)
        //            {
        //                var previousPixel = prevFastBitmap.GetPixel(x, bitmap.Height - y - 1);
        //                capturePixel.Blend(previousPixel, ViewModel.Smoothing);
        //            }

        //            capture.SetPixel(x, bitmap.Height - y - 1, capturePixel);
        //        }
        //    }

        //    if (prevFastBitmap != null)
        //    {
        //        prevFastBitmap.UnlockBitmap();
        //        prevFastBitmap.Dispose();
        //    }

        //    capture.UnlockBitmap();
        //    previousBitmap = (Bitmap)capture.Bitmap.Clone();

        //    capture.LockBitmap();

        //    for (y = 0; y < capture.Bitmap.Height; y++)
        //    {
        //        for (x = 0; x < capture.Bitmap.Width; x++)
        //        {
        //            var px = capture.GetPixel(x, bitmap.Height - y - 1,
        //                ViewModel.BChannel,
        //                ViewModel.GChannel,
        //                ViewModel.RChannel
        //            );

        //            px.SetHue(ViewModel.Hue);
        //            px.SetLuminance(ViewModel.Brightness);
        //            px.SetSaturation(ViewModel.Saturation);

        //            capture.SetPixel(x, bitmap.Height - y - 1, px);
        //        }
        //    }

        //    capture.UnlockBitmap();

        //    var b = (Bitmap)capture.Bitmap.Clone();
        //    capture.Dispose();

        //    return b;
        //}

        byte Interpolate(byte destination, byte source, float p)
        {
            return (byte)((destination * p) + source * (1 - p));
        }

        private void PostProcessImage()
        {
            HLSColor hslColor;
            Color rgbColor;
            int offset, x, y;

            if (postProcessedBuffer.Length != captureService.GetBuffer.Length)
                Array.Resize(ref postProcessedBuffer, captureService.GetBuffer.Length);

            if (postProcessedBackBuffer.Length != captureService.GetBuffer.Length)
                Array.Resize(ref postProcessedBackBuffer, captureService.GetBuffer.Length);

            for (y = 0; y < height; y++)
            {
                for (x = 0; x < width; x++)
                {
                    offset = (x + y * width) * 4;

                    if (ViewModel.EnableSmoothing)
                    {
                        postProcessedBackBuffer[offset + 2] = Interpolate(captureService.GetBuffer[offset + 2], postProcessedBackBuffer[offset + 2], ViewModel.Smoothing);
                        postProcessedBackBuffer[offset + 1] = Interpolate(captureService.GetBuffer[offset + 1], postProcessedBackBuffer[offset + 1], ViewModel.Smoothing);
                        postProcessedBackBuffer[offset + 0] = Interpolate(captureService.GetBuffer[offset + 0], postProcessedBackBuffer[offset + 0], ViewModel.Smoothing);

                        hslColor = new HLSColor(
                           postProcessedBackBuffer[offset + 2],
                           postProcessedBackBuffer[offset + 1],
                           postProcessedBackBuffer[offset + 0]);
                    }
                    else
                    {
                        hslColor = new HLSColor(
                          captureService.GetBuffer[offset + 2],
                          captureService.GetBuffer[offset + 1],
                          captureService.GetBuffer[offset + 0]);
                    }

                    hslColor.hue += (int)(ViewModel.Hue * 240);
                    hslColor.luminosity = (int)(ViewModel.Brightness * hslColor.luminosity);
                    hslColor.saturation = (int)(ViewModel.Saturation * hslColor.saturation);

                    rgbColor = HLSColor.ColorFromHLS(hslColor.hue, hslColor.luminosity, hslColor.saturation);
                    postProcessedBuffer[offset + 2] = rgbColor.R;
                    postProcessedBuffer[offset + 1] = rgbColor.G;
                    postProcessedBuffer[offset + 0] = rgbColor.B;
                }
            }
        }
    }
}
