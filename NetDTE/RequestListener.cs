using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using EnvDTE;
using NetDTE.Handlers;

namespace NetDTE
{
    class RequestListener
    {
        private readonly HttpListener httpListener;
        private IDictionary<string, RequestHandler> handlers;
        private readonly DTE dte;
        private System.Threading.Thread listenerThread;

        public RequestListener(int port, DTE dte)
        {
            this.dte = dte;
            this.httpListener = new HttpListener();
            this.handlers = this.DiscoverHandlers();

            this.Port = port;
        }

        /// <summary>
        /// Gets the port that the listener is running on
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Gets whether or not the listener is currently listening
        /// </summary>
        public bool IsListening
        {
            get { return this.httpListener.IsListening; }
        }

        /// <summary>
        /// Gets a value which indicates whether or not the listener thread is running
        /// </summary>
        public bool IsRunning { get; private set; }

        public void Start()
        {
            if (this.listenerThread != null) return;

            Logger.WriteLine("Starting the http listener thread");

            this.listenerThread = new System.Threading.Thread(new ThreadStart(this.Listen));
            this.listenerThread.Start();
        }

        void Listen()
        {
            this.IsRunning = true;

            // Todo: put the port into configuration
            this.handlers.Keys
                .Select(path => $"http://localhost:{this.Port}{path}/")
                .ToList()
                .ForEach(url => this.httpListener.Prefixes.Add(url));

            Logger.WriteLine($"Starting listener on http://localhost:{this.Port}");

            this.httpListener.GetType().InvokeMember(
                "RemoveAll",
                BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Instance,
                null, this.httpListener, new object[] { false });

            this.httpListener.Start();

            while (this.httpListener.IsListening)
            {
                try
                {
                    var context = this.httpListener.GetContext();
                    RequestHandler handler;

                    if (this.handlers.ContainsKey(context.Request.RawUrl))
                        handler = this.handlers[context.Request.RawUrl];
                    else
                        handler = new RequestHandler(this.dte);

                    handler.HandleRequest(context);
                }
                catch (HttpListenerException)
                {
                }
            }

            Logger.WriteLine("Stopping listener");

            this.IsRunning = false;
        }

        public void Stop()
        {
            if (this.listenerThread == null) return;

            Logger.WriteLine("Stopping the http listener thread..");

            this.httpListener.Stop();
            this.listenerThread.Join(5000);
            this.listenerThread = null;

            Logger.WriteLine("Listener stopped");
        }

        public void SetPort(int port)
        {
            if (this.listenerThread != null || this.httpListener.IsListening)
            {
                Logger.WriteLine("Cannot change the port while the listener is running");
            }

            this.Port = port;
        }

        IDictionary<string, RequestHandler> DiscoverHandlers()
        {
            return this.GetType().Assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<RequestHandlerAttribute>() != null)
                .ToDictionary(t => t.GetCustomAttribute<RequestHandlerAttribute>().baseUrl, t => Activator.CreateInstance(t, this.dte) as RequestHandler);
        }
    }
}
