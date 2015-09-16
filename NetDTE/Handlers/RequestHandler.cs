using System.Net;

namespace NetDTE.Handlers
{
    class RequestHandler
    {
        public virtual void HandleRequest(HttpListenerContext context)
        {
            context.Response.Close();
        }
    }
}
