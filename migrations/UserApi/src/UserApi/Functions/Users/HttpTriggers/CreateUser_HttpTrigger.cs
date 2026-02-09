using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using UserApi.Library.Application.DTOs;
using UserApi.Library.Application.Interfaces;
using UserApi.Library.Application.Validators;
using UserApi.Library.Domain.Entities;

namespace UserApi.Functions.Users.HttpTriggers;

public class CreateUser_HttpTrigger
{
    private readonly IUserService _userService;
    private readonly CreateUserRequestValidator _validator;
    private readonly ILogger<CreateUser_HttpTrigger> _logger;

    public CreateUser_HttpTrigger(
        IUserService userService,
        CreateUserRequestValidator validator,
        ILogger<CreateUser_HttpTrigger> logger)
    {
        _userService = userService;
        _validator = validator;
        _logger = logger;
    }

    [Function("CreateUser")]
    [OpenApiOperation(operationId: "CreateUser", tags: new[] { "Users" },
        Summary = "Create a new user",
        Description = "Creates a new user record with the provided details.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateUserRequest),
        Required = true, Description = "User creation payload")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json",
        bodyType: typeof(User),
        Description = "Successfully created user.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json",
        bodyType: typeof(ValidationResult),
        Description = "Bad request or validation error.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError,
        Description = "Internal server error.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users")] HttpRequestData req)
    {
        _logger.LogInformation("Creating new user");

        try
        {
            var request = await req.ReadFromJsonAsync<CreateUserRequest>();

            var validationResult = _validator.Validate(request);
            if (!validationResult.IsValid)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new
                {
                    message = "Validation failed",
                    errors = validationResult.Errors
                });
                return badRequest;
            }

            var user = await _userService.CreateUserAsync(request!);

            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Location", $"/api/users/{user.Id}");
            await response.WriteAsJsonAsync(user);
            return response;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating user");
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync(ex.Message);
            return badRequest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }
}
