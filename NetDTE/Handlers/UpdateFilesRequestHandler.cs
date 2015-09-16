using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NetDTE.Handlers
{
    [RequestHandler("/project/files")]
    class UpdateFilesRequestHandler : RequestHandler
    {
        public override void HandleRequest(HttpListenerContext context)
        {
            using (var writer = new StreamWriter(context.Response.OutputStream))
            {
                writer.Write("Hello");
            }

            context.Response.OutputStream.Close();
        }
    }
}
