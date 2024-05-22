using Microsoft.AspNetCore.Http;
using Serilog;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestResponseLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        // Log Request
        context.Request.EnableBuffering();
        var requestBody = await ReadRequestBody(context.Request);
        Log.Information("Request: {Method} {Path} {Headers} {Body}",
            context.Request.Method, context.Request.Path, context.Request.Headers, requestBody);

        // Copy original response body stream
        var originalBodyStream = context.Response.Body;

        using (var responseBody = new MemoryStream())
        {
            context.Response.Body = responseBody;

            await _next(context);

            // Log Response
            var responseBodyContent = await ReadResponseBody(context.Response);
            Log.Information("Response: {StatusCode} {Headers} {Body}",
                context.Response.StatusCode, context.Response.Headers, responseBodyContent);

            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private async Task<string> ReadRequestBody(HttpRequest request)
    {
        request.Body.Position = 0;
        using (var reader = new StreamReader(request.Body, leaveOpen: true))
        {
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            return body;
        }
    }

    private async Task<string> ReadResponseBody(HttpResponse response)
    {
        response.Body.Position = 0;
        using (var reader = new StreamReader(response.Body, leaveOpen: true))
        {
            var body = await reader.ReadToEndAsync();
            response.Body.Position = 0;
            return body;
        }
    }
}
