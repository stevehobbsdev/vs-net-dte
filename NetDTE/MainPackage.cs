//------------------------------------------------------------------------------
// <copyright file="MainPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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
    [ProvideAutoLoad(UIContextGuids80.NoSolution)]
    public sealed class MainPackage : Package
    {
        public const string PackageGuidString = "51eb2410-ea28-478a-818c-483927b6b3d4";

        private RequestListener requestListener;

        private DTE2 dte;
        private Events2 events;
        private IList<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        private SolutionEvents solutionEvents;

        public SettingsHandler settings { get; private set; }

        public static AssetCache AssetCache { get; private set; }

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

            Logger.Initialise();

            this.dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            this.events = this.dte.Events as Events2;
            this.solutionEvents = this.events.SolutionEvents;
            
            this.solutionEvents.Opened += SolutionEvents_Opened;
            this.solutionEvents.AfterClosing += Solution_AfterClosed;
        }

        //protected override void Dispose(bool disposing)
        //{
        //    if (this.requestListener != null)
        //        this.requestListener.Stop();
                        
        //    base.Dispose(disposing);
        //}

        //private void StartListener()
        //{
        //    AssetCache = new AssetCache(this.dte);
        //    AssetCache.Initialise();

        //    Logger.WriteLine($"Loading settings from package file");
        //    this.settings = SettingsHandler.LoadFromNodePackageFile(this.dte);

        //    if (settings.IsValid)
        //    {
        //        Logger.WriteLine("Settings loaded");
        //        Logger.WriteLine("NOTE: You must reopen the solution for changes to the settings to take effect");

        //        /*this.requestListener = new RequestListener(this.settings.Port, this.dte);
        //        this.requestListener.Start();*/
        //    }
        //    else
        //    {
        //        Logger.WriteLine("No settings were found or were not valid. Sleeping...");
        //    }
        //}

        //private void StopListener()
        //{
        //    //this.requestListener.Stop();

        //    AssetCache.Clear();
        //    AssetCache = null;
        //}

        private void ProjectItemsEvents_ItemRenamed(ProjectItem projectItem, string oldName)
        {
            AssetCache.Remove(oldName);

            if (AssetCache.ShouldCache(projectItem))
                AssetCache.Add(projectItem);
        }

        private void ProjectItemsEvents_ItemRemoved(ProjectItem projectItem)
        {
            AssetCache.Remove(projectItem);
        }

        private void ProjectItemsEvents_ItemAdded(ProjectItem projectItem)
        {
            if (AssetCache.ShouldCache(projectItem))
                AssetCache.Add(projectItem);
        }

        private void SolutionEvents_Opened()
        {
            AssetCache = new AssetCache(this.dte);
            AssetCache.Initialise();

            var validProjects = SolutionHelper.FindWebProjects(this.dte);

            foreach (var project in validProjects)
            {
                var directory = Path.Combine(Path.GetDirectoryName(project.FullName), "CDN");
                var watcher = new FileSystemWatcher(directory, "*.css");
                var parent = project.ProjectItems;

                watcher.IncludeSubdirectories = true;

                watcher.Created += (sender, args) =>
                {
                    ProcessCssFile(parent, args.FullPath);
                    Logger.WriteLine($"Added { args.Name } to the project");
                };

                watcher.Changed += (sender, args) =>
                {
                    ProcessCssFile(parent, args.FullPath);
                    Logger.WriteLine($"Processed { args.Name }");
                };

                watcher.EnableRaisingEvents = true;

                Logger.WriteLine($"Set up a file watcher on { directory }");

                this.watchers.Add(watcher);
            }

            if (!validProjects.Any())
            {
                Logger.WriteLine($"Didn't find any web projects to watch in the current solution.");
            }

            if (this.events != null)
            {
                this.events.ProjectItemsEvents.ItemAdded += ProjectItemsEvents_ItemAdded;
                this.events.ProjectItemsEvents.ItemRemoved += ProjectItemsEvents_ItemRemoved;
                this.events.ProjectItemsEvents.ItemRenamed += ProjectItemsEvents_ItemRenamed;
            }
            else
            {
                Logger.WriteLine("Could not register events (object was null). Exiting");
                return;
            }
        }

        private static void ProcessCssFile(ProjectItems parent, string cssPath)
        {
            var sassPath = $"{Path.Combine(Path.GetDirectoryName(cssPath), Path.GetFileNameWithoutExtension(cssPath))}.scss";
            var sassProjectItem = AssetCache.Lookup(sassPath);

            if (sassProjectItem != null)
                parent = sassProjectItem.ProjectItems;

            parent.AddFromFile(cssPath);
        }

        private void Solution_AfterClosed()
        {
            //this.StopListener();

            foreach (var watcher in this.watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            this.watchers.Clear();
        }

        #endregion
    }
}
