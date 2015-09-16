using System.Net;
using EnvDTE;
using EnvDTE80;

namespace NetDTE.Handlers
{
    class RequestHandler
    {
        protected DTE DTE;

        public RequestHandler(DTE dte)
        {
            this.DTE = dte;            
        }

        public virtual void HandleRequest(HttpListenerContext context)
        {
            context.Response.Close();
        }
    }
}
