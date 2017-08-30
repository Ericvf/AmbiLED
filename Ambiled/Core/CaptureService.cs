using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
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

        private const int FrameTime = 1000 / 60;
        private Stopwatch frameTimer;

        private Stopwatch fpsTimer;
        private int fpsTotal = 0;
        private int fps = 0;

        static byte[] postProcessedBackBuffer = new byte[0];
        static byte[] postProcessedBuffer = new byte[0];
        int width = 16;
        int height = 16;
        int screen = 0;

        [ImportingConstructor]
        public CaptureService(IViewModel viewModel)
        {
            captureService = FastScreenCaptureService.GetInstance();

            screen = viewModel.MonitorTwo ? 1 : 0;
            width = viewModel.Columns;
            height = viewModel.Rows;

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
                        writeableBitmap.WritePixels(
                            new Int32Rect(0, 0, width, height),
                            captureService.GetBuffer, 
                            width * 4, 
                            0);

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
                Thread.Sleep(FrameTime);
                return;
            }

            if (ViewModel.EnablePostprocessing)
            {
                PostProcessImage();
                ControllerService.Send(postProcessedBuffer, width, height);
            }
            else
                ControllerService.Send(captureService.GetBuffer, width, height);

            if (useFrameTimer)
            {
                frameTimer.Stop();
                Thread.Sleep(Math.Max(0, FrameTime - (int)frameTimer.ElapsedMilliseconds));
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

        byte Interpolate(byte destination, byte source, float p)
        {
            return (byte)((destination * p) + source * (1 - p));
        }

        void PostProcessImage()
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
