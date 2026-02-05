# Naming Conventions & Standards

## 1. General Rules

- Use **PascalCase** for all class names, files, and folders.
- Use **camelCase** for private fields, local variables, and parameters.
- Prefix interfaces with uppercase `I` (e.g., `ILoggerService`).
- Prefix base classes with `Base` (e.g., `BaseTrigger`).
- Use meaningful, descriptive names -- avoid abbreviations.
- File names must match the class name exactly.

## 2. Class Naming

| Type             | Convention                                  | Example                                    | Description                                              |
|------------------|---------------------------------------------|--------------------------------------------|----------------------------------------------------------|
| Trigger Class    | `[ActionName]_[TriggerType]Trigger.cs`      | `CreateFund_HttpTrigger.cs`                | Entry function; inherits from a Base Trigger             |
| Orchestrator     | `[ActionName]_Orchestrator.cs`              | `FundDeletion_Orchestrator.cs`             | Coordinates activities for complex workflows             |
| Activity         | `[ActionName]_[ActivityName]Activity.cs`    | `FundDeletion_ValidateActivity.cs`         | Individual step within an orchestration                  |
| Service Class    | `[EntityName]Service.cs`                    | `FundService.cs`                           | Contains business logic for that entity or domain        |
| Interface        | `I[EntityName][Purpose].cs`                 | `IFundRepository.cs`                       | Contracts for repositories, services, or abstractions    |
| DTO              | `[EntityName][Purpose]Dto.cs`               | `GrantSummaryDto.cs`                       | Transferring structured data between layers              |
| Validator        | `[EntityName][Purpose]Validator.cs`         | `GrantCreateValidator.cs`                  | Validation logic for entity operations                   |
| Enum             | `[EntityName][Type]`                        | `FundStatus.cs`                            | Closed set of values                                     |
| Base Class       | `Base[Purpose].cs`                          | `BaseTrigger.cs`                           | Shared foundational logic for derived classes            |

## 3. Function & Variable Naming

| Scope            | Convention    | Example                        | Description                                |
|------------------|---------------|--------------------------------|--------------------------------------------|
| Public Method    | PascalCase    | `GetFundById()`, `ValidateGrant()` | Action-oriented, descriptive names     |
| Private Method   | camelCase     | `calculateBalance()`, `buildResponse()` | Verb-based, internal behavior       |
| Private Field    | _camelCase    | `_fundRepository`, `_logger`   | Prefix with underscore                     |
| Local Variable   | camelCase     | `fundDetails`, `grantSummary`  | Short, descriptive, scoped locally         |
| Constant         | PascalCase    | `MaxFundLimit`, `DefaultCurrency` | Fixed values in Constants/              |
| Parameter        | camelCase     | `fundId`, `grantDto`           | Descriptive, no abbreviations              |
| Enum Member      | PascalCase    | `Active`, `Inactive`, `Suspended` | Clear names, no prefixes or underscores |

## 4. Trigger Class Patterns

All triggers must inherit from the relevant base class under `Base/Trigger/`.

| Trigger Type        | Base Class              | Example Implementation                                        |
|---------------------|-------------------------|---------------------------------------------------------------|
| HTTP Trigger        | BaseHttpTrigger         | `Fund_Create_HttpTrigger : BaseHttpTrigger`                   |
| Service Bus Trigger | BaseServiceBusTrigger   | `Grant_ApprovedPostInRen_ServiceBusTrigger : BaseServiceBusTrigger` |
| Timer Trigger       | BaseTimerTrigger        | `Fund_DailySummary_TimerTrigger : BaseTimerTrigger`           |
| Blob Trigger        | BaseBlobTrigger         | `Fund_ProcessUpload_BlobTrigger : BaseBlobTrigger`            |

**Naming Pattern**: `[Domain]_[Action][OptionalContext]_[TriggerType]`

Each derived trigger should handle minimal logic: request parsing, validation, and passing data to the Application Service.

## 5. DTO Guidelines

- Keep DTOs flat and simple (no business logic).
- **Request DTOs** -- used for API input.
- **Other DTOs** -- used for responses or entity mapping, should mirror table structure.
- Use nullable fields or `JsonIgnore` for optional data.
- Match naming: `XxxRequest`, `XxxResponse`, `XxxDto`.

### DTO Folder Structure
```
Application/DTOs/
├── GrantDetails/
│   └── GrantDetailsRequest.cs
│       └── Grant.cs          // reference from common library if needed
```

## 6. OpenAPI Standards

### Operation Definition
Every function must include `[OpenApiOperation]`:
- Operation ID must match the function name.
- Tags represent domain groupings (plural form): `"CharitableFunds"`, `"Grants"`, `"Payments"`.
- Provide a clear Summary (one sentence) and concise Description.

### Parameters
Use `[OpenApiParameter]` for all route/query inputs:
- Parameter name must match route placeholder (e.g., `{id}`).
- Always specify: `In`, `Required`, `Type`, `Description`.

### Request Body
Use `[OpenApiRequestBody]`:
- Content type: `"application/json"`.
- Type points to request DTO from `Application/DTOs`.
- Mark `Required = true` if function cannot execute without it.

### Responses
Use `[OpenApiResponseWithBody]`:
- Define both success (200) and error (400, 404, 500) responses.
- Use consistent wrapper: `HttpCommonResponse<T>`.
- Keep descriptions clear.
