using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Windows;

namespace Ambiled.Core
{
    public interface IHttpServer
    {
        void Start();
    }

    public class HttpServer : IHttpServer
    {
        const string httpPrefix = "http://+:81/";
        const int httpPort = 81;

        public event Action<HttpListenerContext> ProcessRequest;

        readonly HttpListener Listener = new HttpListener();

        public void Start()
        {
            try
            {
                // netsh http add urlacl url="http://+:81/" user=everyone
                Listener.Prefixes.Add(httpPrefix);
                Listener.Start();
                Task.Run(() => Listen());
            }
            catch (HttpListenerException)
            {
                MessageBox.Show(
                    $"An error occured while registering the HTTP listener." + Environment.NewLine +
                    $"Please make sure you have a ACL entry for port {httpPort}." + Environment.NewLine + Environment.NewLine +
                    $"E.g. netsh http add urlacl url=\"http://+:{httpPort}/\" user=everyone"
                    , "Webserver error",
                    MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        async void Listen()
        {
            while (true)
            {
                var context = await Listener.GetContextAsync();
                HandleRequest(context);
            }
        }

        void HandleRequest(HttpListenerContext context)
        {
            ProcessRequest?.Invoke(context);
        }
    }

    [Export(typeof(IHttpServer))]
    public class WebServer : HttpServer
    {
        [Import]
        public IViewModel ViewModel { get; set; }

        [Import]
        public IControllerService ControllerService { get; set; }

        [Import]
        public ILogger Logger { get; set; }

        public WebServer()
            : base()
        {
            ProcessRequest += WebServer_ProcessRequest;
        }

        private void WebServer_ProcessRequest(HttpListenerContext context)
        {
            if (!context.Request.Url.LocalPath.Equals("/"))
                return;

            context.Response.AddHeader("Access-Control-Allow-Origin", "*");

            if (context.Request.HttpMethod == "GET")
            {
                HandleGet(context);
            }
            else if (context.Request.HttpMethod == "POST")
            {
                HandlePost(context);
            }

            context.Response.Close();
        }

        private void HandleGet(HttpListenerContext context)
        {
            if (context.Request.Url.Query == "?")
            {
                Logger.Add("Webclient: index.html/?");

                var currentState = new
                {
                    Usb = ControllerService.IsOpen(),

                    EnableAuto3d = ViewModel.EnableAuto3d,
                    Is3DOff = ViewModel.Is3DOff,
                    Is3DSBS = ViewModel.Is3DSBS,
                    Is3DOU = ViewModel.Is3DOU,

                    EnableFixedFPS = ViewModel.EnableFixedFPS,
                    EnablePostprocessing = ViewModel.EnablePostprocessing,
                    EnableCrop = ViewModel.EnableCrop,

                    EnableSmoothing = ViewModel.EnableSmoothing,
                    Smoothing = ViewModel.Smoothing,

                    Brightness = ViewModel.Brightness,
                    Hue = ViewModel.Hue,
                    Saturation = ViewModel.Saturation,

                    ViewModel.EnableGamma,
                };

                var json = Json.Encode(currentState);
                WriteOutput(context, json);

            }
            else
            {
                var content = File.ReadAllText("index.html");
                WriteOutput(context, content);

                Logger.Add("Webclient: index.html");
            }
        }

        private void HandlePost(HttpListenerContext context)
        {
            float AsFloat(dynamic value) => (float)(Convert.ToDouble(value) / 100);
            bool AsBool(dynamic value) => Convert.ToBoolean(value);

            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                var body = reader.ReadToEnd();
                var payload = Json.Decode(body);

                Logger.Add($"Webserver: {payload.id}={payload.value}");
                switch (payload.id)
                {
                    case "Usb":
                        if (payload.value)
                            ControllerService.Start();
                        else
                            ControllerService.Stop();
                        break;

                    case "EnableCrop":
                        ViewModel.EnableCrop = AsBool(payload.value);
                        break;

                    case "EnableAuto3d":
                        ViewModel.EnableAuto3d = AsBool(payload.value);
                        break;

                    case "Is3DOff":
                        ViewModel.Is3DOff = AsBool(payload.value);
                        break;

                    case "Is3DSBS":
                        ViewModel.Is3DSBS = AsBool(payload.value);
                        break;

                    case "Is3DOU":
                        ViewModel.Is3DOU = AsBool(payload.value);
                        break;

                    case "EnablePostprocessing":
                        ViewModel.EnablePostprocessing = AsBool(payload.value);
                        break;

                    case "EnableFixedFPS":
                        ViewModel.EnableFixedFPS = AsBool(payload.value);
                        break;

                    case "EnableSmoothing":
                        ViewModel.EnableSmoothing = AsBool(payload.value);
                        break;

                    case "Smoothing":
                        ViewModel.Smoothing = AsFloat(payload.value);
                        break;

                    case "Brightness":
                        ViewModel.Brightness = AsFloat(payload.value);
                        break;

                    case "Hue":
                        ViewModel.Hue = AsFloat(payload.value);
                        break;

                    case "Saturation":
                        ViewModel.Saturation = AsFloat(payload.value);
                        break;

                    case "EnableGamma":
                        ViewModel.EnableGamma = AsBool(payload.value);
                        break;

                    default:
                        break;
                }
            }
        }

        private static void WriteOutput(HttpListenerContext context, string content)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
        }
    }
}
