using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Newtonsoft.Json;

namespace NetDTE.Handlers
{
    [RequestHandler("/project/files")]
    class UpdateFilesRequestHandler : RequestHandler
    {
        public UpdateFilesRequestHandler(DTE dte)
            : base(dte)
        {
        }

        public override void HandleRequest(HttpListenerContext context)
        {
            using (var reader = new StreamReader(context.Request.InputStream))
            {
                var json = reader.ReadToEnd();
                var files = JsonConvert.DeserializeObject<string[]>(json);

                files.ToList().ForEach(f =>
                {
                    var item = SolutionHelper.FindSolutionItemByName(this.DTE, f, true);
                });
            }

            using (var writer = new StreamWriter(context.Response.OutputStream))
            {
                writer.Write("Hello");
            }

            context.Response.OutputStream.Close();
        }
    }
}
