using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Templates
{
    public struct TemplateScopeContext
    {
        public PageResult PageResult { get; }
        public TemplatePage Page => PageResult.Page;
        public TemplatePagesContext Context => Page.Context;
        public Dictionary<string, object> ScopedParams { get; internal set; }
        public Stream OutputStream { get; }

        public TemplateScopeContext(PageResult pageResult, Stream outputStream, Dictionary<string, object> scopedParams)
        {
            PageResult = pageResult;
            ScopedParams = scopedParams ?? new Dictionary<string, object>();
            OutputStream = outputStream;
        }
    }

    public static class TemplateScopeContextUtils
    {
        public static TemplateScopeContext CreateScopedContext(this TemplateScopeContext scope, string template)
        {
            var dynamicPage = scope.Context.OneTimePage(template);
            var pageResult = scope.PageResult.Clone(dynamicPage).Init().Result;
            var itemScope = new TemplateScopeContext(pageResult, scope.OutputStream, new Dictionary<string, object>(scope.ScopedParams));

            return itemScope;
        }

        public static async Task WritePageAsync(this TemplateScopeContext scope)
        {
            await scope.PageResult.WritePageAsync(scope.Page, scope);            
        }

        public static TemplateScopeContext ScopeWithParams(this TemplateScopeContext parentContext, Dictionary<string, object> scopedParams)
        {
            if (scopedParams == null)
                return parentContext;

            if (parentContext.ScopedParams.Count == 0)
                return new TemplateScopeContext(parentContext.PageResult, parentContext.OutputStream, scopedParams);
            
            var to = new Dictionary<string, object>();
            foreach (var entry in parentContext.ScopedParams)
            {
                to[entry.Key] = entry.Value;
            }
            foreach (var entry in scopedParams)
            {
                to[entry.Key] = entry.Value;
            }
            return new TemplateScopeContext(parentContext.PageResult, parentContext.OutputStream, to);
        }
        
        public static TemplateScopeContext ScopeWithStream(this TemplateScopeContext scope, Stream stream) =>
            new TemplateScopeContext(scope.PageResult, stream, scope.ScopedParams);

        public static async Task WritePageAsync(this TemplateScopeContext scope, TemplatePage page, Dictionary<string, object> pageParams, CancellationToken token = default(CancellationToken))
        {
            await scope.PageResult.WritePageAsync(page, scope.ScopeWithParams(pageParams), token);
        }
    }
}