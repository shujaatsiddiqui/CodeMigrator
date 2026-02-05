# Code Templates - SerenitySupport Clean Architecture

Use these templates when scaffolding new projects or migrating legacy code.
Replace `{Entity}`, `{Domain}`, `{Action}` with actual names.

---

## Application Layer

### Service Class
```csharp
// Location: Application/Services/{Entity}Service.cs
public class {Entity}Service : I{Entity}Service
{
    private readonly I{Entity}Repository _repository;

    public {Entity}Service(I{Entity}Repository repository)
    {
        _repository = repository;
    }

    public async Task<{Entity}ResponseDto> Create{Entity}(Create{Entity}RequestDto dto)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("{Entity} name is required");

        // Create domain entity
        var entity = new {Entity}(dto.Name, dto.Amount);

        // Persist
        await _repository.AddAsync(entity);

        // Return response
        return new {Entity}ResponseDto { Id = entity.Id, Name = entity.Name };
    }
}
```

### Service Interface
```csharp
// Location: Application/Interface/I{Entity}Service.cs
public interface I{Entity}Service
{
    Task<{Entity}ResponseDto> Create{Entity}(Create{Entity}RequestDto dto);
    Task<{Entity}ResponseDto> Get{Entity}ById(Guid id);
    Task Update{Entity}(Guid id, Update{Entity}RequestDto dto);
    Task Delete{Entity}(Guid id);
}
```

### Validator
```csharp
// Location: Application/Validators/{Entity}{Action}Validator.cs
public class {Entity}{Action}Validator
{
    public ValidationResult Validate({Action}{Entity}Query query)
    {
        if (string.IsNullOrEmpty(query.{Entity}Id))
            return ValidationResult.Fail("{Entity}Id is required");

        return ValidationResult.Success();
    }
}
```

### Request DTO
```csharp
// Location: Application/DTOs/Create{Entity}RequestDto.cs
public class Create{Entity}RequestDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
```

### Response DTO
```csharp
// Location: Application/DTOs/{Entity}ResponseDto.cs
public class {Entity}ResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

### Constants
```csharp
// Location: Application/Constants/{Entity}Constants.cs
public static class {Entity}Constants
{
    public const decimal MinimumAmount = 1.00m;
    public const string DefaultCurrency = "USD";
}
```

---

## Domain Layer

### Entity
```csharp
// Location: Domain/Entities/{Entity}.cs
public class {Entity}
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public decimal Amount { get; private set; }

    public {Entity}(string name, decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive");

        Name = name;
        Amount = amount;
        Id = Guid.NewGuid();
    }
}
```

### Repository Interface
```csharp
// Location: Domain/Interface/I{Entity}Repository.cs
public interface I{Entity}Repository
{
    Task<{Entity}?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync({Entity} entity, CancellationToken ct = default);
    Task UpdateAsync({Entity} entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
```

### Domain Event
```csharp
// Location: Domain/Events/{Entity}CreatedEvent.cs
public class {Entity}CreatedEvent
{
    public Guid {Entity}Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
```

---

## Infrastructure Layer

### Repository Implementation
```csharp
// Location: Infrastructure/Repositories/{Entity}Repository.cs
public class {Entity}Repository : I{Entity}Repository
{
    private readonly AppDbContext _context;

    public {Entity}Repository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<{Entity}?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.{Entity}s.FindAsync(new object[] { id }, ct);
    }

    public async Task AddAsync({Entity} entity, CancellationToken ct = default)
    {
        _context.{Entity}s.Add(entity);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync({Entity} entity, CancellationToken ct = default)
    {
        _context.{Entity}s.Update(entity);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.{Entity}s.FindAsync(new object[] { id }, ct);
        if (entity != null)
        {
            _context.{Entity}s.Remove(entity);
            await _context.SaveChangesAsync(ct);
        }
    }
}
```

### GraphQL Query
```csharp
// Location: Infrastructure/GraphQL/Queries/Get{Entity}DetailsQuery.cs
public static class Get{Entity}DetailsQuery
{
    public static string Text()
    {
        return @"
            query Get{Entity}Details($id: UUID!) {
                {entity}ById(id: $id) {
                    id
                    name
                    status
                    created_at
                }
            }";
    }
}
```

### GraphQL Mutation
```csharp
// Location: Infrastructure/GraphQL/Mutations/Create{Entity}Mutation.cs
public static class Create{Entity}Mutation
{
    public static string GetMutation()
    {
        return @"
            mutation Create{Entity}($item: {Entity}Input!, $userId: UUID!) {
                create{Entity}(
                    item: {
                        name: $item.name
                        amount: $item.amount
                        created_by_party_id: $userId
                        last_modified_by_party_id: $userId
                    }
                ) {
                    id
                    name
                    created_by_party_id
                    last_modified_by_party_id
                }
            }";
    }
}
```

### GraphQL Mutation with Audit Fields (Update)
```csharp
// Location: Infrastructure/GraphQL/Mutations/Update{Entity}Mutation.cs
public static class Update{Entity}Mutation
{
    public static string GetMutation()
    {
        return @"
            mutation Update{Entity}($id: UUID!, $userId: UUID!) {
                update{Entity}(
                    id: $id
                    item: {
                        last_modified_by_party_id: $userId
                    }
                ) {
                    id
                    last_modified_by_party_id
                }
            }";
    }
}
```

### Repository Using GraphQL
```csharp
// Location: Infrastructure/Repositories/{Entity}ReadRepository.cs
public class {Entity}ReadRepository : I{Entity}ReadRepository
{
    private readonly IGraphQLClient _client;

    public {Entity}ReadRepository(IGraphQLClient client) => _client = client;

    public async Task<Get{Entity}DetailsResponseDto?> GetDetailsAsync(Guid id, CancellationToken ct)
    {
        var query = Get{Entity}DetailsQuery.Text();
        var variables = new { id };
        var resp = await _client.ExecuteAsync(query, variables,
            SerializerCtx.GraphQLResponseGet{Entity}DetailsResponseDto, ct);
        return resp?.data;
    }
}
```

### Repository Using IRequestContext for Audit
```csharp
// Location: Infrastructure/Repositories/{Entity}WriteRepository.cs
public class {Entity}WriteRepository : I{Entity}WriteRepository
{
    private readonly IGraphQLClient _graphQLRepository;
    private readonly IRequestContext _requestContext;

    public {Entity}WriteRepository(IGraphQLClient graphQLRepository, IRequestContext requestContext)
    {
        _graphQLRepository = graphQLRepository;
        _requestContext = requestContext;
    }

    public async Task<{Entity}Dto> Update{Entity}(Guid id, CancellationToken cancellationToken)
    {
        var mutationParameters = new
        {
            id = id,
            userId = _requestContext.UserId
        };

        var graphqlResponse = await _graphQLRepository.ExecuteGraphqlQuery<GraphQLResponse<Update{Entity}Response>>(
            Update{Entity}Mutation.GetMutation(),
            mutationParameters,
            SerializerCtx.GraphQLResponseUpdate{Entity}Response,
            cancellationToken);

        return graphqlResponse.data;
    }
}
```

---

## Triggers (Entry Point)

### HTTP Trigger
```csharp
// Location: Functions/{Domain}/HttpTriggers/Create{Entity}_HttpTrigger.cs
public class Create{Entity}_HttpTrigger : BaseHttpTrigger
{
    private readonly I{Entity}Service _service;

    public Create{Entity}_HttpTrigger(I{Entity}Service service)
    {
        _service = service;
    }

    [FunctionName("Create{Entity}_Http")]
    [OpenApiOperation("Create{Entity}_Http", tags: new[] { "{Domain}" },
        Summary = "Create a new {Entity}",
        Description = "Creates a new {Entity} record with the provided details.")]
    [OpenApiRequestBody("application/json", typeof(Create{Entity}RequestDto),
        Required = true, Description = "{Entity} creation payload")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json",
        typeof(HttpCommonResponse<{Entity}ResponseDto>),
        Description = "Successfully created {Entity}.")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json",
        typeof(HttpCommonResponse<{Entity}ResponseDto>),
        Description = "Bad request or validation error.")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "{entity}")] HttpRequest req)
    {
        var dto = await req.ReadFromJsonAsync<Create{Entity}RequestDto>();
        var result = await _service.Create{Entity}(dto);
        return new OkObjectResult(new HttpCommonResponse<{Entity}ResponseDto> { Data = result });
    }
}
```

### HTTP Trigger with Path Parameter
```csharp
// Location: Functions/{Domain}/HttpTriggers/Get{Entity}ById_HttpTrigger.cs
public class Get{Entity}ById_HttpTrigger : BaseHttpTrigger
{
    private readonly I{Entity}Service _service;

    public Get{Entity}ById_HttpTrigger(I{Entity}Service service)
    {
        _service = service;
    }

    [FunctionName("Get{Entity}ById_Http")]
    [OpenApiOperation("Get{Entity}ById_Http", tags: new[] { "{Domain}" },
        Summary = "Get {Entity} by ID",
        Description = "Retrieves a {Entity} record by its unique identifier.")]
    [OpenApiParameter("id", In = ParameterLocation.Path, Required = true,
        Type = typeof(string), Description = "{Entity} ID (GUID)")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json",
        typeof(HttpCommonResponse<{Entity}ResponseDto>),
        Description = "{Entity} details.")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json",
        typeof(HttpCommonResponse<{Entity}ResponseDto>),
        Description = "Bad request or validation error.")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "{entity}/{id}")] HttpRequest req,
        string id)
    {
        var result = await _service.Get{Entity}ById(Guid.Parse(id));
        return new OkObjectResult(new HttpCommonResponse<{Entity}ResponseDto> { Data = result });
    }
}
```

### Service Bus Trigger
```csharp
// Location: Functions/{Domain}/ServiceBusTriggers/{Entity}Approved_ServiceBusTrigger.cs
public class {Entity}Approved_ServiceBusTrigger : BaseServiceBusTrigger
{
    private readonly I{Entity}Service _service;

    public {Entity}Approved_ServiceBusTrigger(I{Entity}Service service)
    {
        _service = service;
    }

    [FunctionName("{Entity}Approved_ServiceBus")]
    public async Task Run(
        [ServiceBusTrigger("%{Entity}ApprovedQueue%", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        var dto = JsonSerializer.Deserialize<{Entity}ApprovedDto>(message.Body);
        await _service.Handle{Entity}Approved(dto);
        await messageActions.CompleteMessageAsync(message);
    }
}
```

### Timer Trigger
```csharp
// Location: Functions/{Domain}/HttpTriggers/{Entity}_DailySummary_TimerTrigger.cs
public class {Entity}_DailySummary_TimerTrigger : BaseTimerTrigger
{
    private readonly I{Entity}Service _service;

    public {Entity}_DailySummary_TimerTrigger(I{Entity}Service service)
    {
        _service = service;
    }

    [FunctionName("{Entity}_DailySummary_Timer")]
    public async Task Run(
        [TimerTrigger("%{Entity}DailySummaryCron%")] TimerInfo timer)
    {
        await _service.GenerateDailySummary();
    }
}
```

---

## Orchestration (Durable Functions)

### Orchestrator
```csharp
// Location: Functions/{Domain}/Orchestrations/{ActionName}/{ActionName}_Orchestrator.cs
public class {ActionName}_Orchestrator
{
    [FunctionName("{ActionName}_Orchestrator")]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var input = context.GetInput<{ActionName}InputDto>();

        // Step 1: Validate
        var isValid = await context.CallActivityAsync<bool>(
            "{ActionName}_ValidateActivity", input);

        if (!isValid)
            throw new InvalidOperationException("Validation failed");

        // Step 2: Perform action
        await context.CallActivityAsync("{ActionName}_PerformActivity", input);
    }
}
```

### Activity
```csharp
// Location: Functions/{Domain}/Orchestrations/{ActionName}/Activities/{ActionName}_ValidateActivity.cs
public class {ActionName}_ValidateActivity
{
    private readonly I{Entity}Service _service;

    public {ActionName}_ValidateActivity(I{Entity}Service service)
    {
        _service = service;
    }

    [FunctionName("{ActionName}_ValidateActivity")]
    public async Task<bool> Run(
        [ActivityTrigger] {ActionName}InputDto input)
    {
        return await _service.Validate(input);
    }
}
```

### Orchestration HTTP Trigger
```csharp
// Location: Functions/{Domain}/Orchestrations/{ActionName}/Triggers/{ActionName}_TriggerByHttp.cs
public class {ActionName}_TriggerByHttp : BaseHttpTrigger
{
    private readonly IDurableClient _durableClient;

    public {ActionName}_TriggerByHttp(IDurableClient durableClient)
    {
        _durableClient = durableClient;
    }

    [FunctionName("{ActionName}_TriggerByHttp")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "{action}")] HttpRequest req,
        [DurableClient] IDurableOrchestrationClient starter)
    {
        var input = await req.ReadFromJsonAsync<{ActionName}InputDto>();
        var instanceId = await starter.StartNewAsync("{ActionName}_Orchestrator", input);
        return starter.CreateCheckStatusResponse(req, instanceId);
    }
}
```

---

## Message Publishing

### Using IServiceBusProducer
```csharp
// IServiceBusProducer automatically picks UserId from IRequestContext
// and includes it in Application Properties of the message.
public class {Entity}Service : I{Entity}Service
{
    private readonly IServiceBusProducer _messageBus;

    public {Entity}Service(IServiceBusProducer messageBus)
    {
        _messageBus = messageBus;
    }

    public async Task Publish{Entity}Event({Entity}EventDto eventDto)
    {
        await _messageBus.SendAsync(eventDto);
    }
}
```

---

## IRequestContext Usage

```csharp
// IRequestContext is a scoped service holding current request info.
// Use it for audit fields instead of passing user details through every method.
public class {Entity}WriteRepository
{
    private readonly IRequestContext _requestContext;

    public {Entity}WriteRepository(IRequestContext requestContext)
    {
        _requestContext = requestContext;
    }

    public async Task Create{Entity}()
    {
        var mutationParameters = new
        {
            userId = _requestContext.UserId    // auto-populates audit fields
        };
    }
}
```
