using System;
using System.IO;
using EnvDTE;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;

namespace NetDTE
{
    public class SettingsHandler
    {
        public bool IsValid { get; set; }

        public int Port { get; set; }

        public static SettingsHandler LoadFromNodePackageFile(DTE dte)
        {
            if (dte == null)
            {
                throw new ArgumentNullException("dte");
            }

            var packageFileItem = SolutionHelper.FindSolutionItemByName(dte, "package.json", true, SearchType.Filename);

            if (packageFileItem != null)
            {
                var path = (string)packageFileItem.Properties.Item("FullPath").Value;
                var settings = new SettingsHandler();

                using (var fs = new FileStream(path, FileMode.Open))
                using (var reader = new StreamReader(fs))
                {
                    try
                    {
                        var contents = reader.ReadToEnd();
                        var packageFile = JsonConvert.DeserializeObject<dynamic>(contents);

                        if (packageFile.notifyDte != null)
                        {
                            if (packageFile.port != null)
                                settings.Port = packageFile.notifyDte.port;
                        }
                    }
                    catch (RuntimeBinderException)
                    {
                    }

                    // Primitive validation, will upgrade when there are more settings
                    settings.IsValid = settings.Port > 0;

                    return settings;
                }
            }

            return null;
        }
    }
}
