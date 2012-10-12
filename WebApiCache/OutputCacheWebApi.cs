using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Caching;
using System.Threading;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace WebApiCache
{
    public class OutputCacheWebApi : ActionFilterAttribute
    {
        private static readonly ObjectCache WebApiCache = MemoryCache.Default;
        private bool _anonymousOnly;
        private string _cacheKey;
        private bool _filterByAbsoluteUri;
        private int _timeSpan;

        public OutputCacheWebApi(int timeSpan = 60, bool anonymousOnly = false, bool filterByAbsoluteUri = true)
        {
            _timeSpan = timeSpan;
            _anonymousOnly = anonymousOnly;
            _filterByAbsoluteUri = filterByAbsoluteUri;
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            if (!(WebApiCache.Contains(_cacheKey)))
            {
                var body = actionExecutedContext.Response.Content.ReadAsStringAsync().Result;
                WebApiCache.Add(_cacheKey, body, DateTime.Now.AddSeconds(_timeSpan));
                WebApiCache.Add(_cacheKey + ":response-ct", actionExecutedContext.Response.Content.Headers.ContentType,
                                DateTime.Now.AddSeconds(_timeSpan));
            }

            if (IsCacheable(actionExecutedContext.ActionContext))
                actionExecutedContext.ActionContext.Response.Headers.CacheControl = SetClientCache();
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (actionContext == null)
                throw new ArgumentNullException("actionContext");
            if (!IsCacheable(actionContext)) return;

            SetCacheKey(actionContext);

            if (!WebApiCache.Contains(_cacheKey)) return;

            var val = (string)WebApiCache.Get(_cacheKey);

            if (val == null) return;

            actionContext.Response = actionContext.Request.CreateResponse();
            actionContext.Response.Content = new StringContent(val);
            var contentType = (MediaTypeHeaderValue)WebApiCache.Get(_cacheKey + ":response-ct") ??
                              new MediaTypeHeaderValue(_cacheKey.Split(':')[1]);
            actionContext.Response.Content.Headers.ContentType = contentType;
            actionContext.Response.Headers.CacheControl = SetClientCache();
        }

        private bool IsCacheable(HttpActionContext actionContext)
        {
            if (_timeSpan > 0)
            {
                if (_anonymousOnly)
                    if (Thread.CurrentPrincipal.Identity.IsAuthenticated) return false;
                if (actionContext.Request.Method == HttpMethod.Get) return true;
            }
            else
                throw new InvalidOperationException("Wrong Arguments");
            return false;
        }

        private void SetCacheKey(HttpActionContext actionContext)
        {
            if (_filterByAbsoluteUri)
            {
                _cacheKey = string.Join(":",
                                    new[]
                                        {
                                            actionContext.Request.RequestUri.AbsoluteUri,
                                            actionContext.Request.Headers.Accept.FirstOrDefault().ToString()
                                        });
            }
            else
            {
                _cacheKey = string.Join(":",
                                    new[]
                                        {
                                            actionContext.Request.RequestUri.AbsolutePath,
                                            actionContext.Request.Headers.Accept.FirstOrDefault().ToString()
                                        });
            }
        }

        private CacheControlHeaderValue SetClientCache()
        {
            return new CacheControlHeaderValue { MaxAge = TimeSpan.FromSeconds(_timeSpan), MustRevalidate = true };
        }
    }
}