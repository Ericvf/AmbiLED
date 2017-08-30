using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
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

        JavaScriptSerializer serializer = new JavaScriptSerializer();

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


                var json = serializer.Serialize(currentState);
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
            float AsFloat(object value) => (float)(Convert.ToDouble(value) / 100);
            bool AsBool(object value) => Convert.ToBoolean(value);

            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                var body = reader.ReadToEnd();

                var payload = serializer.Deserialize<Dictionary<string, object>>(body);
                var id = payload["id"].ToString();
                var value = payload["value"];

                Logger.Add($"Webserver: {id}={value}");
                switch (payload["id"])
                {
                    case "Usb":
                        if (AsBool(value))
                            ControllerService.Start();
                        else
                            ControllerService.Stop();
                        break;

                    case "EnableCrop":
                        ViewModel.EnableCrop = AsBool(value);
                        break;

                    case "EnableAuto3d":
                        ViewModel.EnableAuto3d = AsBool(value);
                        break;

                    case "Is3DOff":
                        ViewModel.Is3DOff = AsBool(value);
                        break;

                    case "Is3DSBS":
                        ViewModel.Is3DSBS = AsBool(value);
                        break;

                    case "Is3DOU":
                        ViewModel.Is3DOU = AsBool(value);
                        break;

                    case "EnablePostprocessing":
                        ViewModel.EnablePostprocessing = AsBool(value);
                        break;

                    case "EnableFixedFPS":
                        ViewModel.EnableFixedFPS = AsBool(value);
                        break;

                    case "EnableSmoothing":
                        ViewModel.EnableSmoothing = AsBool(value);
                        break;

                    case "Smoothing":
                        ViewModel.Smoothing = AsFloat(value);
                        break;

                    case "Brightness":
                        ViewModel.Brightness = AsFloat(value);
                        break;

                    case "Hue":
                        ViewModel.Hue = AsFloat(value);
                        break;

                    case "Saturation":
                        ViewModel.Saturation = AsFloat(value);
                        break;

                    case "EnableGamma":
                        ViewModel.EnableGamma = AsBool(value);
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
