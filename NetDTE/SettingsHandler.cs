using System;
using System.IO;
using EnvDTE;
using EnvDTE80;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;

namespace NetDTE
{
    public class SettingsHandler
    {
        public bool IsValid { get; set; }

        public int Port { get; set; }

        public static SettingsHandler LoadFromNodePackageFile(DTE2 dte)
        {
            if (dte == null)
            {
                throw new ArgumentNullException("dte");
            }

            var packageFileItem = SolutionHelper.FindSolutionItemByName(dte, "package.json", true, SearchType.Filename);
            var settings = new SettingsHandler();

            if (packageFileItem != null)
            {
                var path = (string)packageFileItem.Properties.Item("FullPath").Value;

                using (var fs = new FileStream(path, FileMode.Open))
                using (var reader = new StreamReader(fs))
                {
                    try
                    {
                        var contents = reader.ReadToEnd();
                        var packageFile = JsonConvert.DeserializeObject<dynamic>(contents);

                        if (packageFile.notifyDte != null)
                        {
                            if (packageFile.notifyDte.port != null)
                                settings.Port = packageFile.notifyDte.port;
                            else
                                Logger.WriteLine("port setting for notifyDte not found");
                        }
                        else
                            Logger.WriteLine("No notifyDte package element found");
                    }
                    catch (RuntimeBinderException e)
                    {
                        Logger.WriteLine($"There was an error (${e.GetType().Name}) loading the package file: ${e.Message}");
                    }

                    // Primitive validation, will upgrade when there are more settings
                    settings.IsValid = settings.Port > 0;

                    return settings;
                }
            }
            else
            {
                Logger.WriteLine("package.json not found");
            }

            return settings;
        }
    }
}
