using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using UserApi.Library.Application.Interfaces;
using UserApi.Library.Domain.Entities;

namespace UserApi.Functions.Users.HttpTriggers;

public class GetUserById_HttpTrigger
{
    private readonly IUserService _userService;
    private readonly ILogger<GetUserById_HttpTrigger> _logger;

    public GetUserById_HttpTrigger(IUserService userService, ILogger<GetUserById_HttpTrigger> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [Function("GetUserById")]
    [OpenApiOperation(operationId: "GetUserById", tags: new[] { "Users" },
        Summary = "Get user by ID",
        Description = "Retrieves a user record by its unique identifier.")]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true,
        Type = typeof(string), Description = "User ID (GUID)")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json",
        bodyType: typeof(User),
        Description = "User details.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound,
        Description = "User not found.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest,
        Description = "Invalid user ID format.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError,
        Description = "Internal server error.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{id}")] HttpRequestData req,
        string id)
    {
        _logger.LogInformation("Getting user by id: {Id}", id);

        if (!Guid.TryParse(id, out var userId))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Invalid user ID format");
            return badRequest;
        }

        try
        {
            var user = await _userService.GetUserByIdAsync(userId);

            if (user == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(user);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by id: {Id}", id);
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }
}
