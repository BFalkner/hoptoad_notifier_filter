using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;
using Yaml;

namespace MVCHelpers.Filters {
    public class Hoptoad : ActionFilterAttribute
    {
        private readonly HoptoadResource resource = new HoptoadResource();

        public Hoptoad(string apiKey)
        {
            resource.ApiKey = apiKey;
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext.Exception != null)
                SendException("Action", filterContext.Exception, filterContext);

            base.OnActionExecuted(filterContext);
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (filterContext.Exception != null)
                SendException("Result", filterContext.Exception, filterContext);

            base.OnResultExecuted(filterContext);
        }

        private void SendException(string type, Exception exception, ControllerContext context)
        {
            resource.ErrorMessage = Extract(exception, e => e.InnerException, e => e.Message)
                .Aggregate(new StringBuilder(type + " Error:\r\n"), (sb, m) => sb.AppendLine(m)).ToString();
            resource.Backtrace = Extract(exception, e => e.InnerException, e => e.StackTrace.Split('\n'))
                .SelectMany(a => a).ToList();

            resource.Request["RoutePath"] = ((Route)context.RouteData.Route).Url;
            resource.Request["Route"] = context.RouteData.Values;
            resource.Request["Method"] = context.HttpContext.Request.HttpMethod;
            resource.Request["Form"] = context.HttpContext.Request.Form;
            resource.Request["QueryString"] = context.HttpContext.Request.QueryString;

            var session = context.HttpContext.Session;
            foreach (string k in session.Keys) resource.Session.Add(k, session[k]);

            resource.Send();
        }

        private static List<R> Extract<T, R>(T source, Func<T, T> next, Func<T, R> extract)
            where T : class
        {
            T n = next(source);
            List<R> list = n != null ? Extract(n, next, extract) : new List<R>();
            list.Add(extract(source));
            return list;
        }

        private class HoptoadResource
        {
            public string ApiKey { get; set; }
            public string ErrorMessage { get; set; }
            public List<string> Backtrace { get; set; }
            public IDictionary<string, object> Request { get; set; }
            public IDictionary<string, object> Session { get; set; }
            public IDictionary<string, object> Environment { get; set; }

            public HoptoadResource()
            {
                ApiKey = "";
                ErrorMessage = "";
                Backtrace = new List<string>();
                Request = new Dictionary<string, object>();
                Session = new Dictionary<string, object>();
                Environment = new Dictionary<string, object>();
            }

            public void Send()
            {
                var data = ComposeRequest();

                var req = WebRequest.Create("http://hoptoadapp.com/notices/");
                req.ContentType = "application/x-yaml";
                req.Method = "POST";
                req.ContentLength = data.Length;
                var stream = req.GetRequestStream();
                stream.Write(data, 0, data.Length);
                stream.Close();

                var res = req.GetResponse();
                // Ignore response...
                res.Close();
            }

            private byte[] ComposeRequest() {
                var req = new Document(new[] {new HashNode("notice", new Mapping {
                    {"api_key", ApiKey},
                    {"error_message", ErrorMessage},
                    {"backtrace", Backtrace},
                    {"request", FormatHash(Request)},
                    {"session", FormatHash(Session)},
                    {"environment", FormatHash(Environment)}
                })}).ToString();

                return Encoding.UTF8.GetBytes(req);
            }

            private static Mapping FormatHash(IDictionary<string, object> hash)
            {
                return hash.Aggregate(new Mapping(), (m, p) =>
                    IsHash(p.Value) ?
                        m.Add(p.Key, FormatHash(ToHash(p.Value))) :
                        m.Add(p.Key, p.Value.ToString()));
            }

            private static bool IsHash(object obj)
            {
                if (obj is IDictionary) return true;
                if (obj is IDictionary<string, object>) return true;
                if (obj is NameValueCollection) return true;
                return false;
            }

            private static IDictionary<string, object> ToHash(object obj)
            {
                if (obj is IDictionary)
                {
                    if (obj is IDictionary<string, object>) return (IDictionary<string, object>)obj;
                    var dict = (IDictionary) obj;
                    var ret = new Dictionary<string, object>();
                    foreach (object k in dict.Keys) ret[k.ToString()] = dict[k];
                    return ret;
                }
                if (obj is IDictionary<string, object>)
                    return (IDictionary<string, object>) obj;

                if (obj is NameValueCollection)
                {
                    var col = (NameValueCollection) obj;
                    var ret = new Dictionary<string, object>();
                    foreach (string k in col.Keys) ret[k] = col[k];
                    return ret;
                }
                throw new ArgumentOutOfRangeException("obj", string.Format("Type \"{0}\" not supported.", obj.GetType()));
            }
        }
    }

}
