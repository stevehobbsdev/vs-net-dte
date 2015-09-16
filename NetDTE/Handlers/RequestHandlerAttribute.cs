using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetDTE.Handlers
{
    public class RequestHandlerAttribute : Attribute
    {
        public string baseUrl;

        public RequestHandlerAttribute(string path)
        {
            this.baseUrl = path;
        }

    }
}
