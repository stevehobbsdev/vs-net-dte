using System.Net;
using EnvDTE;
using EnvDTE80;

namespace NetDTE.Handlers
{
    class RequestHandler
    {
        protected DTE2 DTE;

        public RequestHandler(DTE2 dte)
        {
            this.DTE = dte;            
        }

        public virtual void HandleRequest(HttpListenerContext context)
        {
            context.Response.Close();
        }
    }
}
