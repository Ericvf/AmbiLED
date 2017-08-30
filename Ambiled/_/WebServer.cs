using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Helpers;

namespace Ambiled.Core
{
    class HttpServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly Thread _listenerThread;
        private readonly Thread[] _workers;
        private readonly ManualResetEvent _stop, _ready;
        private Queue<HttpListenerContext> _queue;

        public HttpServer(int maxThreads)
        {
            _workers = new Thread[maxThreads];
            _queue = new Queue<HttpListenerContext>();
            _stop = new ManualResetEvent(false);
            _ready = new ManualResetEvent(false);
            _listener = new HttpListener();
            _listenerThread = new Thread(HandleRequests);
        }

        public void Start(int port)
        {
            // netsh http add urlacl url="http://+:81/" user=everyone
            _listener.Prefixes.Add(String.Format(@"http://+:{0}/", port));
            _listener.Start();
            _listenerThread.Start();

            for (int i = 0; i < _workers.Length; i++)
            {
                _workers[i] = new Thread(Worker);
                _workers[i].Start();
            }
        }

        public void Dispose()
        { Stop(); }

        public void Stop()
        {
            _stop.Set();
            _listenerThread.Join();
            foreach (Thread worker in _workers)
                worker.Join();
            _listener.Stop();
        }

        private void HandleRequests()
        {
            while (_listener.IsListening)
            {
                var context = _listener.BeginGetContext(ContextReady, null);

                if (0 == WaitHandle.WaitAny(new[] { _stop, context.AsyncWaitHandle }))
                    return;
            }
        }

        private void ContextReady(IAsyncResult ar)
        {
            try
            {
                lock (_queue)
                {
                    Debug.WriteLine("Queueing");
                    _queue.Enqueue(_listener.EndGetContext(ar));
                    _ready.Set();
                }
            }
            catch { return; }
        }

        private void Worker()
        {
            WaitHandle[] wait = new[] { _ready, _stop };
            while (0 == WaitHandle.WaitAny(wait))
            {
                HttpListenerContext context;
                lock (_queue)
                {
                    if (_queue.Count > 0)
                        context = _queue.Dequeue();
                    else
                    {
                        _ready.Reset();
                        continue;
                    }
                }

                try { ProcessRequest(context); }
                catch (Exception e) { Console.Error.WriteLine(e); }
            }
        }

        public event Action<HttpListenerContext> ProcessRequest;
    }

    class AmbiledServer : HttpServer
    {
        public AmbiledServer()
            : base(1)
        {
            this.ProcessRequest += AmbiledServer_ProcessRequest;
        }

        void AmbiledServer_ProcessRequest(HttpListenerContext context)
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
                var message = new
                {
                    //Usb = App.VM.IsConnected(),

                    Off = !App.VM.EnableCapture,
                    EnableCapture = App.VM.EnableCapture,

                    EnableAuto3d = App.VM.EnableAuto3d,
                    Is3DOff = App.VM.Is3DOff,
                    Is3DSBS = App.VM.Is3DSBS,
                    Is3DOU = App.VM.Is3DOU,

                    EnableFixedFPS = App.VM.EnableFixedFPS,
                    EnablePostprocessing = App.VM.EnablePostprocessing,
                    EnableCrop = App.VM.EnableCrop,

                    EnableSmoothing = App.VM.EnableSmoothing,
                    Smoothing = App.VM.Smoothing,

                    Brightness = App.VM.Brightness,
                    Hue = App.VM.Hue,
                    Saturation = App.VM.Saturation,

                    App.VM.EnableGamma,
                };


                var json = Json.Encode(message);
                WriteOutput(context, json);
            }
            else
            {
                var path = "index.html";
                if (!File.Exists(path))
                    path = @"..\..\index.html";

                var content = File.ReadAllText(path);
                WriteOutput(context, content);
            }
        }

        private void HandlePost(HttpListenerContext context)
        {
            using (var reader = new StreamReader(
                context.Request.InputStream,
                context.Request.ContentEncoding))
            {
                var text = reader.ReadToEnd();
                var obj = Json.Decode(text);

                string call = obj.id;

                //Messages.AddMessage(string.Format("Remote: {0} : {1}", obj.id, obj.value));


                switch (call)
                {
                    case "Usb":
                        //if (obj.value)
                        //    App.VM.StartController();
                        //else
                        //    App.VM.StopController();
                        //break;

                    case "Off":
                        App.VM.EnableCapture = false;
                        break;

                    case "EnableCrop":
                        App.VM.EnableCrop = obj.value;
                        break;

                    case "EnableCapture":
                        App.VM.EnableCapture = obj.value;
                        break;

                    case "EnableAuto3d":
                        App.VM.EnableAuto3d = obj.value;
                        break;

                    case "Is3DOff":
                        App.VM.Is3DOff = true;
                        break;

                    case "Is3DSBS":
                        App.VM.Is3DSBS = true;
                        break;

                    case "Is3DOU":
                        App.VM.Is3DOU = true;
                        break;

                    case "EnablePostprocessing":
                        App.VM.EnablePostprocessing = obj.value;
                        break;

                    case "EnableFixedFPS":
                        App.VM.EnableFixedFPS = obj.value;
                        break;

                    case "EnableSmoothing":
                        App.VM.EnableSmoothing = obj.value;
                        break;

                    case "Smoothing":
                        App.VM.Smoothing = (float)obj.value;
                        break;


                    case "Brightness":
                        App.VM.Brightness = (float)obj.value;
                        break;

                    case "Hue":
                        App.VM.Hue = (float)obj.value;
                        break;

                    case "Saturation":
                        App.VM.Saturation = (float)obj.value;
                        break;

                    case "EnableGamma":
                        App.VM.EnableGamma = obj.value;
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

    public class WebServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly Func<HttpListenerRequest, string> _responderMethod;

        public WebServer(string[] prefixes, Func<HttpListenerRequest, string> method)
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException(
                    "Needs Windows XP SP2, Server 2003 or later.");

            // URI prefixes are required, for example 
            // "http://localhost:8080/index/".
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");

            // A responder method is required
            if (method == null)
                throw new ArgumentException("method");

            foreach (string s in prefixes)
                _listener.Prefixes.Add(s);

            _responderMethod = method;
            _listener.Start();
        }

        public WebServer(Func<HttpListenerRequest, string> method, params string[] prefixes)
            : this(prefixes, method) { }

        public void Run()
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Debug.WriteLine("Webserver running");
                try
                {
                    while (_listener.IsListening)
                    {
                        Debug.WriteLine("Listing");

                        ThreadPool.QueueUserWorkItem((c) =>
                        {
                            Debug.WriteLine("Response");
                            var ctx = c as HttpListenerContext;
                            try
                            {
                                string rstr = _responderMethod(ctx.Request);
                                byte[] buf = Encoding.UTF8.GetBytes(rstr);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            }
                            catch { } // suppress any exceptions
                            finally
                            {
                                // always close the stream
                                ctx.Response.OutputStream.Close();
                            }
                        }, _listener.GetContext());
                    }
                }
                catch { } // suppress any exceptions
            });
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }
    }

    //public sealed class DynamicJsonConverter : JavaScriptConverter
    //{
    //    public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
    //    {
    //        if (dictionary == null)
    //            throw new ArgumentNullException("dictionary");

    //        return type == typeof(object) ? new DynamicJsonObject(dictionary) : null;
    //    }

    //    public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override IEnumerable<Type> SupportedTypes
    //    {
    //        get { return new ReadOnlyCollection<Type>(new List<Type>(new[] { typeof(object) })); }
    //    }

    //    #region Nested type: DynamicJsonObject

    //    private sealed class DynamicJsonObject : DynamicObject
    //    {
    //        private readonly IDictionary<string, object> _dictionary;

    //        public DynamicJsonObject(IDictionary<string, object> dictionary)
    //        {
    //            if (dictionary == null)
    //                throw new ArgumentNullException("dictionary");
    //            _dictionary = dictionary;
    //        }

    //        public override string ToString()
    //        {
    //            var sb = new StringBuilder("{");
    //            ToString(sb);
    //            return sb.ToString();
    //        }

    //        private void ToString(StringBuilder sb)
    //        {
    //            var firstInDictionary = true;
    //            foreach (var pair in _dictionary)
    //            {
    //                if (!firstInDictionary)
    //                    sb.Append(",");
    //                firstInDictionary = false;
    //                var value = pair.Value;
    //                var name = pair.Key;
    //                if (value is string)
    //                {
    //                    sb.AppendFormat("{0}:\"{1}\"", name, value);
    //                }
    //                else if (value is IDictionary<string, object>)
    //                {
    //                    new DynamicJsonObject((IDictionary<string, object>)value).ToString(sb);
    //                }
    //                else if (value is ArrayList)
    //                {
    //                    sb.Append(name + ":[");
    //                    var firstInArray = true;
    //                    foreach (var arrayValue in (ArrayList)value)
    //                    {
    //                        if (!firstInArray)
    //                            sb.Append(",");
    //                        firstInArray = false;
    //                        if (arrayValue is IDictionary<string, object>)
    //                            new DynamicJsonObject((IDictionary<string, object>)arrayValue).ToString(sb);
    //                        else if (arrayValue is string)
    //                            sb.AppendFormat("\"{0}\"", arrayValue);
    //                        else
    //                            sb.AppendFormat("{0}", arrayValue);

    //                    }
    //                    sb.Append("]");
    //                }
    //                else
    //                {
    //                    sb.AppendFormat("{0}:{1}", name, value);
    //                }
    //            }
    //            sb.Append("}");
    //        }

    //        public override bool TryGetMember(GetMemberBinder binder, out object result)
    //        {
    //            if (!_dictionary.TryGetValue(binder.Name, out result))
    //            {
    //                // return null to avoid exception.  caller can check for null this way...
    //                result = null;
    //                return true;
    //            }

    //            result = WrapResultObject(result);
    //            return true;
    //        }

    //        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
    //        {
    //            if (indexes.Length == 1 && indexes[0] != null)
    //            {
    //                if (!_dictionary.TryGetValue(indexes[0].ToString(), out result))
    //                {
    //                    // return null to avoid exception.  caller can check for null this way...
    //                    result = null;
    //                    return true;
    //                }

    //                result = WrapResultObject(result);
    //                return true;
    //            }

    //            return base.TryGetIndex(binder, indexes, out result);
    //        }

    //        private static object WrapResultObject(object result)
    //        {
    //            var dictionary = result as IDictionary<string, object>;
    //            if (dictionary != null)
    //                return new DynamicJsonObject(dictionary);

    //            var arrayList = result as ArrayList;
    //            if (arrayList != null && arrayList.Count > 0)
    //            {
    //                return arrayList[0] is IDictionary<string, object>
    //                    ? new List<object>(arrayList.Cast<IDictionary<string, object>>().Select(x => new DynamicJsonObject(x)))
    //                    : new List<object>(arrayList.Cast<object>());
    //            }

    //            return result;
    //        }
    //    }

    //    #endregion
    //}
}
