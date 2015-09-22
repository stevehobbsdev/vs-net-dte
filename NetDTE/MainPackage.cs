//------------------------------------------------------------------------------
// <copyright file="MainPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NetDTE.Handlers;

namespace NetDTE
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(MainPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string)]
    public sealed class MainPackage : Package
    {
        public const string PackageGuidString = "51eb2410-ea28-478a-818c-483927b6b3d4";
        public const string OutputWindowGuidString = "938b8f16-a80b-5e17-e61d-e858251e66d0";

        private IDictionary<string, RequestHandler> handlers;
        private HttpListener httpListener;
        private System.Threading.Thread processorThread;

        private DTE dte;
        private IVsOutputWindow outWindow;
        private IVsOutputWindowPane customPane;

        public SettingsHandler settings { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPackage"/> class.
        /// </summary>
        public MainPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            this.InitialiseOutputWindow();

            this.dte = (DTE)this.GetService(typeof(DTE));

            WriteLineToOutput($"Loading settings from package file");
            this.settings = SettingsHandler.LoadFromNodePackageFile(this.dte);

            if (settings.IsValid)
            {
                WriteLineToOutput("Settings loaded");
                StartListener();
            }
            else
            {
                WriteLineToOutput($"No settings were found. Sleeping...");
            }
        }

        private void InitialiseOutputWindow()
        {
            this.outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

            // Use e.g. Tools -> Create GUID to make a stable, but unique GUID for your pane.
            // Also, in a real project, this should probably be a static constant, and not a local variable
            // http://stackoverflow.com/questions/1094366/how-do-i-write-to-the-visual-studio-output-window-in-my-custom-tool
            var guid = new Guid(OutputWindowGuidString);
            var title = "EnvDTE";

            this.outWindow.CreatePane(ref guid, title, 1, 1);
            
            outWindow.GetPane(ref guid, out this.customPane);
        }

        protected override void Dispose(bool disposing)
        {
            if (this.httpListener != null)
                this.httpListener.Stop();

            if (this.processorThread != null)
                this.processorThread.Join(5000);
                        
            base.Dispose(disposing);
        }

        IDictionary<string, RequestHandler> DiscoverHandlers()
        {
            return this.GetType().Assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<RequestHandlerAttribute>() != null)
                .ToDictionary(t => t.GetCustomAttribute<RequestHandlerAttribute>().baseUrl, t => Activator.CreateInstance(t, this.dte) as RequestHandler);
        }

        void HandleListener()
        {
            this.httpListener = new HttpListener();

            int port = this.settings.Port;
            
            // Todo: put the port into configuration
            this.handlers.Keys
                .Select(path => $"http://localhost:{port}{path}/")
                .ToList()
                .ForEach(url => this.httpListener.Prefixes.Add(url));

            WriteLineToOutput($"Starting listener on http://localhost:{port}");

            this.httpListener.Start();          

            while (this.httpListener.IsListening)
            {
                try
                {
                    var context = this.httpListener.GetContext();
                    var handler = new RequestHandler(this.dte);

                    if (this.handlers.ContainsKey(context.Request.RawUrl))
                        handler = this.handlers[context.Request.RawUrl];

                    handler.HandleRequest(context);
                }
                catch(HttpListenerException)
                {
                }
            }

            WriteLineToOutput($"Stopping listener");

            this.httpListener = null;
        }

        private void StartListener()
        {
            if (this.processorThread != null) return;

            this.handlers = this.DiscoverHandlers();

            this.processorThread = new System.Threading.Thread(new ThreadStart(this.HandleListener));
            this.processorThread.Start();
        }

        private void StopListener()
        {
            if (this.processorThread == null) return;

            this.httpListener.Stop();
            this.processorThread.Join(5000);
            this.processorThread = null;
        }

        private void WriteLineToOutput(string message)
        {
            if (this.customPane == null) return;

            if (!message.EndsWith("\n"))
                message = message + "\n";

            this.customPane.OutputString(message);
        }

        #endregion
    }
}
