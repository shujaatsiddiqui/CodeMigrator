# Clean Architecture Reference - SerenitySupport Pattern

## Core Principles

- **Dependency Rule**: All dependencies point inward. Core logic never depends on frameworks, databases, or UI.
- **Independence of Frameworks**: Replace frameworks without rewriting logic.
- **Testability**: Business logic can be tested in isolation.
- **Independence of UI**: Change UI without touching core code.
- **Independence of Database**: Swap databases without changing logic.
- **Separation of Concerns**: Each layer has a single clear purpose.

---

## Project Structure (3 Projects)

### 1. SerenitySupport (Entry Point - Azure Functions)

```
SerenitySupport/
├── Base/
│   └── Trigger/
│       ├── BaseTrigger.cs                  // Common base for all triggers
│       ├── BaseHttpTrigger.cs              // Inherits from BaseTrigger
│       ├── BaseServiceBusTrigger.cs        // Inherits from BaseTrigger
│       ├── BaseTimerTrigger.cs             // Inherits from BaseTrigger
│       └── BaseBlobTrigger.cs              // For blob events
│
└── Functions/
    └── {DomainName}/                       // e.g., Funds, Grants, Payments
        │
        ├── Orchestrations/                 // Complex multi-step workflows
        │   └── {ActionName}/               // e.g., FundDeletion
        │       ├── Activities/
        │       │   ├── {ActionName}_ValidateActivity.cs
        │       │   └── {ActionName}_PerformActivity.cs
        │       ├── Triggers/
        │       │   ├── {ActionName}_TriggerByHttp.cs
        │       │   ├── {ActionName}_TriggerByTimer.cs
        │       │   ├── {ActionName}_TriggerByQueue.cs
        │       │   └── {ActionName}_TriggerByBlob.cs
        │       └── {ActionName}_Orchestrator.cs
        │
        ├── HttpTriggers/                   // Simple HTTP-triggered functions (no orchestration)
        │   ├── Create{Entity}_HttpTrigger.cs
        │   ├── Update{Entity}_HttpTrigger.cs
        │   └── Delete{Entity}_HttpTrigger.cs
        │
        └── ServiceBusTriggers/             // Simple service bus-triggered functions (no orchestration)
            ├── {Entity}Approved_ServiceBusTrigger.cs
            └── {Entity}Rejected_ServiceBusTrigger.cs
```

### 2. SerenitySupport.{Domain}.Library (Business Logic)

```
SerenitySupport.{Domain}.Library/
│
├── Application/
│   ├── Services/           // Main business logic (FundService, GrantService)
│   ├── DTOs/               // Request/Response models
│   ├── Validators/         // Input validation classes
│   ├── Constants/          // Messages, limits, default values
│   ├── Strategies/         // Alternate logic paths (e.g., activity types)
│   ├── Interface/          // Service contracts (IFundService)
│   └── Behaviors/          // Logging or validation helpers (optional)
│
├── Domain/
│   ├── Entities/           // Core business objects (Fund, Grant)
│   ├── Events/             // Domain events (FundCreatedEvent)
│   └── Interface/          // Repository contracts (IFundRepository)
│
├── Infrastructure/
│   ├── Repositories/       // Data access implementations
│   ├── Configurations/     // EF Core or ORM mappings
│   ├── Serializers/        // Data format handling
│   ├── ExternalServices/   // APIs, message bus, etc.
│   └── GraphQL/
│       ├── Queries/        // e.g., GetOfferingDetailsQuery.cs
│       └── Mutations/      // e.g., CreateCharitableFundMutation.cs
│
└── Common/
    ├── Constants/          // Domain-specific shared constants
    └── Enums/              // Domain-specific shared enums (FundStatus)
```

### 3. SerenitySupport.Common (Shared Across All Domains)

```
SerenitySupport.Common/
│
├── Constants/              // Global reusable constants (error messages, config keys)
├── Enums/                  // Shared enumerations across multiple domains
├── Extensions/             // Extension methods for common data types
├── Helpers/                // Utility classes (logging, retry, validation helpers)
├── Interfaces/             // Shared contracts (loggers, serializers, publishers)
├── JsonConverters/         // Custom JSON converters for consistent serialization
├── Models/                 // Shared DTOs, base models, standard API response types
├── Repositories/           // Generic/base repository abstractions
├── Serializers/            // JSON/XML serialization implementations
├── Services/               // Cross-domain reusable services (HTTP, logging, messaging)
└── DTOs/                   // Common DTOs shared by multiple libraries
    ├── Grant/
    │   └── GrantDto.cs
    └── Party/
        └── PartyAddressDto.cs
```

---

## Layer Responsibilities

### Application Layer
- Contains service classes with use-case logic (create, update, retrieve).
- Depends on Domain layer interfaces (repositories).
- Never depends on Infrastructure directly.
- Contains: Services, DTOs, Validators, Constants, Strategies, Interfaces, Behaviors.

### Domain Layer
- Contains core business entities with their own validation and behavior.
- Contains repository interface contracts.
- Has ZERO external dependencies.
- Contains: Entities, Events, Interfaces.

### Infrastructure Layer
- Implements technical details: database access, external APIs, messaging.
- Implements interfaces defined in Domain layer.
- Contains: Repositories, Configurations, Serializers, ExternalServices, GraphQL.

### Common Layer
- Shared utilities, constants, and models used across multiple domains.
- No business logic.

---

## Dependency Flow

```
Triggers (Entry Point)
    │
    ▼
Application Layer (Services, DTOs, Validators)
    │
    ▼
Domain Layer (Entities, Interfaces)
    ▲
    │
Infrastructure Layer (Repositories, External Services)
```

Infrastructure implements Domain interfaces. Application depends on Domain interfaces.
Triggers call Application services. Nothing depends on Triggers.

---

## Trigger Base Class Inheritance

| Trigger Type       | Base Class              |
|--------------------|-------------------------|
| HTTP Trigger       | BaseHttpTrigger         |
| Service Bus Trigger| BaseServiceBusTrigger   |
| Timer Trigger      | BaseTimerTrigger        |
| Blob Trigger       | BaseBlobTrigger         |

All base classes inherit from `BaseTrigger`.
Each trigger should handle minimal logic: request parsing, validation, and delegation to Application Service.

---

## GraphQL Patterns

### Queries and Mutations Location
- `Infrastructure/GraphQL/Queries/` for read operations
- `Infrastructure/GraphQL/Mutations/` for write operations

### Key Rules
- Use static classes to keep queries reusable without instantiation.
- Use parameterized `$variables` (never hardcode values).
- Return string from a method (not a const) to allow multiline formatting.
- Naming: `[Entity][Action]Query` or `[Entity][Action]Mutation`.
- These live under Infrastructure since they define how data is fetched.
- Application code calls them via the repository interface.

### Audit Fields
- `last_modified_by_party_id` -- for both update and create mutations.
- `created_by_party_id` -- for create mutations only.
- Use `IRequestContext` to populate audit fields.
- `IServiceBusProducer` automatically picks UserId from IRequestContext for message properties.

### Testing locally (Postman)
Add header: `X-CompanyUser-Id: 32e31a14-b6d5-44fe-9de8-ffc1b09001e8` (Default NCF Party Id)
