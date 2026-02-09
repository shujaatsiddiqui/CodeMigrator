using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UserApi.Library.Application.Interfaces;
using UserApi.Library.Application.Services;
using UserApi.Library.Application.Validators;
using UserApi.Library.Domain.Interfaces;
using UserApi.Library.Infrastructure.Repositories;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register DbContext with InMemory database for development
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseInMemoryDatabase("UserApiDb"));

// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Register services
builder.Services.AddScoped<IUserService, UserService>();

// Register validators
builder.Services.AddScoped<CreateUserRequestValidator>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
