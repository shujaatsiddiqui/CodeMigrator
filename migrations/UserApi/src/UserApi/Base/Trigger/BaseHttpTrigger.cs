using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace UserApi.Base.Trigger;

/// <summary>
/// Base class for HTTP triggers providing common functionality.
/// </summary>
public abstract class BaseHttpTrigger
{
    protected readonly ILogger Logger;

    protected BaseHttpTrigger(ILogger logger)
    {
        Logger = logger;
    }

    protected async Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData req,
        HttpStatusCode statusCode,
        string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteStringAsync(message);
        return response;
    }

    protected async Task<HttpResponseData> CreateJsonResponse<T>(
        HttpRequestData req,
        HttpStatusCode statusCode,
        T data)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(data);
        return response;
    }

    protected bool TryParseGuid(string value, out Guid result)
    {
        return Guid.TryParse(value, out result);
    }
}
