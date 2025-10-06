using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace PDI_Azure_Function.Extensions
{
    static class HttpExtenstions
    {
        public static string GetBaseURL(this HttpRequest req)
        {
            UriBuilder uri = new UriBuilder(req.Scheme, req.Host.Host, (req.Host.Port.HasValue ? (int)req.Host.Port : -1), req.Path);

            if (uri.Path.IndexOf("api/") >= 0)
                uri.Path = uri.Path.Substring(0, uri.Path.IndexOf("api/") + 4);

            return uri.ToString();
        }
    }
}
