using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using UserApi.Library.Application.Interfaces;
using UserApi.Library.Domain.Entities;

namespace UserApi.Functions.Users.HttpTriggers;

public class GetAllUsers_HttpTrigger
{
    private readonly IUserService _userService;
    private readonly ILogger<GetAllUsers_HttpTrigger> _logger;

    public GetAllUsers_HttpTrigger(IUserService userService, ILogger<GetAllUsers_HttpTrigger> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [Function("GetAllUsers")]
    [OpenApiOperation(operationId: "GetAllUsers", tags: new[] { "Users" },
        Summary = "Get all users",
        Description = "Retrieves a list of all users in the system.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json",
        bodyType: typeof(IEnumerable<User>),
        Description = "List of all users.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError,
        Description = "Internal server error.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users")] HttpRequestData req)
    {
        _logger.LogInformation("Getting all users");

        try
        {
            var users = await _userService.GetAllUsersAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(users);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }
}
