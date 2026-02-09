using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using UserApi.Library.Application.Interfaces;

namespace UserApi.Functions.Users.HttpTriggers;

public class DeleteUser_HttpTrigger
{
    private readonly IUserService _userService;
    private readonly ILogger<DeleteUser_HttpTrigger> _logger;

    public DeleteUser_HttpTrigger(IUserService userService, ILogger<DeleteUser_HttpTrigger> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [Function("DeleteUser")]
    [OpenApiOperation(operationId: "DeleteUser", tags: new[] { "Users" },
        Summary = "Delete a user",
        Description = "Deletes a user record by its unique identifier. Operation is idempotent.")]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true,
        Type = typeof(string), Description = "User ID (GUID)")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent,
        Description = "User successfully deleted or did not exist.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest,
        Description = "Invalid user ID format.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError,
        Description = "Internal server error.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "users/{id}")] HttpRequestData req,
        string id)
    {
        _logger.LogInformation("Deleting user: {Id}", id);

        if (!Guid.TryParse(id, out var userId))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Invalid user ID format");
            return badRequest;
        }

        try
        {
            await _userService.DeleteUserAsync(userId);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {Id}", id);
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }
}
