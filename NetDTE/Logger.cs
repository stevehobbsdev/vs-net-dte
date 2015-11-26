using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace NetDTE
{
    public static class Logger
    {
        public const string OutputWindowGuidString = "938b8f16-a80b-5e17-e61d-e858251e66d0";

        private static IVsOutputWindow outWindow;
        private static IVsOutputWindowPane customPane;

        public static void Initialise()
        {
            outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

            // Use e.g. Tools -> Create GUID to make a stable, but unique GUID for your pane.
            // Also, in a real project, this should probably be a static constant, and not a local variable
            // http://stackoverflow.com/questions/1094366/how-do-i-write-to-the-visual-studio-output-window-in-my-custom-tool
            var guid = new Guid(OutputWindowGuidString);
            var title = "EnvDTE";

            outWindow.CreatePane(ref guid, title, 1, 1);

            outWindow.GetPane(ref guid, out customPane);
        }

        public static void WriteLine(string line = null)
        {
            if (customPane == null) return;

            if (line != null)
            {
                if (!line.EndsWith("\r\n"))
                    line = $"{line}\r\n";
            }

            customPane.OutputString(line ?? "\r\n");
        }

        public static void WriteToEventLog(Exception exception)
        {
            WriteToEventLog(exception.Message);
        }

        public static void WriteToEventLog(string message)
        {
            string source = "NetDTE";
            string log = "Application";

            if (!EventLog.SourceExists(source))
                EventLog.CreateEventSource(source, log);

            EventLog.WriteEntry(source, message, EventLogEntryType.Error);
        }
    }
}
