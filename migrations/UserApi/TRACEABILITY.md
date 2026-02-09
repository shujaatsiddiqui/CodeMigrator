# Migration Traceability Matrix

## Source: .NET Framework 4.8 Web API
## Target: .NET 9 Azure Functions (Isolated Worker)

---

## Endpoint Mapping

| Legacy Endpoint | Legacy Controller Method | New Function | Status |
|-----------------|-------------------------|--------------|--------|
| GET /api/users | UsersController.Get() | GetAllUsers_HttpTrigger | ✓ |
| GET /api/users/{id} | UsersController.Get(Guid id) | GetUserById_HttpTrigger | ✓ |
| POST /api/users | UsersController.Post(NewUser) | CreateUser_HttpTrigger | ✓ |
| DELETE /api/users/{id} | UsersController.Delete(Guid id) | DeleteUser_HttpTrigger | ✓ |

---

## Business Logic Mapping

| Legacy Component | Legacy Location | New Component | New Location | Status |
|-----------------|-----------------|---------------|--------------|--------|
| User Entity | WebApiExample.DataStore/Models/User.cs | User | Domain/Entities/User.cs | ✓ |
| NewUser DTO | WebApiExample.WebApp/Models/NewUser.cs | CreateUserRequest | Application/DTOs/CreateUserRequest.cs | ✓ |
| IUserService | WebApiExample.WebApp/Services/UserService.cs | IUserService | Application/Interfaces/IUserService.cs | ✓ |
| UserService | WebApiExample.WebApp/Services/UserService.cs | UserService | Application/Services/UserService.cs | ✓ |
| IUnitOfWork | WebApiExample.Common/DataAccess/IUnitOfWork.cs | IUserRepository | Domain/Interfaces/IUserRepository.cs | ✓ |
| UserContext | WebApiExample.DataStore/UserContext.cs | UserDbContext | Infrastructure/Repositories/UserDbContext.cs | ✓ |

---

## Business Rules Mapping

| Rule ID | Description | Legacy Implementation | New Implementation | Test Cases | Status |
|---------|-------------|----------------------|-------------------|------------|--------|
| BR-001 | Name is required | ModelState validation | CreateUserRequestValidator | Given_EmptyName_When_Validate_Then_ReturnsInvalid | ✓ |
| BR-002 | Age is required | ModelState validation | CreateUserRequestValidator | Given_InvalidAge_When_Validate_Then_ReturnsInvalid | ✓ |
| BR-003 | "admin" is banned | UserService._bannedNames | UserService.BannedNames | Given_BannedUsername_When_CreateUserAsync_Then_ThrowsArgumentException | ✓ |
| BR-004 | "sa" is banned | UserService._bannedNames | UserService.BannedNames | Given_BannedUsername_When_CreateUserAsync_Then_ThrowsArgumentException | ✓ |
| BR-005 | Delete is idempotent | UserService.RemoveAsync | UserService.DeleteUserAsync | Given_UserNotFound_When_DeleteUserAsync_Then_DoesNothing | ✓ |

---

## Test Coverage Summary

| Category | Test Count | Status |
|----------|------------|--------|
| UserService Unit Tests | 13 | ✓ |
| CreateUserRequestValidator Tests | 9 | ✓ |
| Integration Tests | 16 | ✓ |
| **Total** | **38** | **All Passing** |

---

## Test Case Details

### UserService Tests

| Test Case | Covers |
|-----------|--------|
| Given_UsersExist_When_GetAllUsersAsync_Then_ReturnsAllUsers | GET /api/users happy path |
| Given_NoUsersExist_When_GetAllUsersAsync_Then_ReturnsEmptyList | GET /api/users empty result |
| Given_DatabaseError_When_GetAllUsersAsync_Then_ExceptionBubblesUp | Error handling |
| Given_UserExists_When_GetUserByIdAsync_Then_ReturnsUser | GET /api/users/{id} happy path |
| Given_UserNotFound_When_GetUserByIdAsync_Then_ReturnsNull | GET /api/users/{id} not found |
| Given_DatabaseError_When_GetUserByIdAsync_Then_ExceptionBubblesUp | Error handling |
| Given_ValidUserRequest_When_CreateUserAsync_Then_CreatesAndReturnsUser | POST /api/users happy path |
| Given_BannedUsername_When_CreateUserAsync_Then_ThrowsArgumentException (admin) | BR-003 |
| Given_BannedUsername_When_CreateUserAsync_Then_ThrowsArgumentException (sa) | BR-004 |
| Given_DatabaseError_When_CreateUserAsync_Then_ExceptionBubblesUp | Error handling |
| Given_UserExists_When_DeleteUserAsync_Then_DeletesUser | DELETE /api/users/{id} happy path |
| Given_UserNotFound_When_DeleteUserAsync_Then_DoesNothing | BR-005 |
| Given_DatabaseErrorOnDelete_When_DeleteUserAsync_Then_ExceptionBubblesUp | Error handling |

### Validator Tests

| Test Case | Covers |
|-----------|--------|
| Given_ValidRequest_When_Validate_Then_ReturnsValid | Valid input |
| Given_EmptyName_When_Validate_Then_ReturnsInvalid (null) | BR-001 |
| Given_EmptyName_When_Validate_Then_ReturnsInvalid ("") | BR-001 |
| Given_EmptyName_When_Validate_Then_ReturnsInvalid (whitespace) | BR-001 |
| Given_InvalidAge_When_Validate_Then_ReturnsInvalid (0) | BR-002 |
| Given_InvalidAge_When_Validate_Then_ReturnsInvalid (-1) | BR-002 |
| Given_AgeTooHigh_When_Validate_Then_ReturnsInvalid | Age validation |
| Given_NullRequest_When_Validate_Then_ReturnsInvalid | Null handling |
| Given_BannedName_When_Validate_Then_ReturnsInvalid | BR-003, BR-004 |

---

## Architecture Comparison

### Legacy (.NET Framework 4.8)

```
WebApiExample.WebApp (MVC + Web API)
    ├── Controllers/
    │   └── UsersController.cs
    ├── Services/
    │   └── UserService.cs
    └── Models/
        └── NewUser.cs

WebApiExample.DataStore (EF6)
    ├── Models/
    │   └── User.cs
    └── UserContext.cs

WebApiExample.Common
    └── DataAccess/
        ├── IRootEntity.cs
        └── IUnitOfWork.cs
```

### New (.NET 9 Azure Functions)

```
UserApi (Azure Functions - Entry Point)
    ├── Base/Trigger/
    │   └── BaseHttpTrigger.cs
    └── Functions/Users/HttpTriggers/
        ├── GetAllUsers_HttpTrigger.cs
        ├── GetUserById_HttpTrigger.cs
        ├── CreateUser_HttpTrigger.cs
        └── DeleteUser_HttpTrigger.cs

UserApi.Library (Clean Architecture)
    ├── Application/
    │   ├── DTOs/
    │   │   ├── CreateUserRequest.cs
    │   │   ├── UserResponse.cs
    │   │   └── ValidationResult.cs
    │   ├── Interfaces/
    │   │   └── IUserService.cs
    │   ├── Services/
    │   │   └── UserService.cs
    │   └── Validators/
    │       └── CreateUserRequestValidator.cs
    ├── Domain/
    │   ├── Entities/
    │   │   └── User.cs
    │   └── Interfaces/
    │       └── IUserRepository.cs
    └── Infrastructure/
        └── Repositories/
            ├── UserDbContext.cs
            └── UserRepository.cs

UserApi.Common (Shared)
    └── (future shared components)
```

---

## Migration Metrics

| Metric | Value |
|--------|-------|
| Legacy Endpoints | 4 |
| Migrated Endpoints | 4 |
| Coverage | 100% |
| Total Tests | 38 |
| Passing Tests | 38 |
| Test Coverage | 100% business logic |

---

## OpenAPI/Swagger Support

All HTTP triggers include OpenAPI attributes for automatic Swagger documentation:

| Endpoint | OpenAPI Attributes |
|----------|-------------------|
| GET /api/users | `[OpenApiOperation]`, `[OpenApiResponseWithBody]` |
| GET /api/users/{id} | `[OpenApiOperation]`, `[OpenApiParameter]`, `[OpenApiResponseWithBody]` |
| POST /api/users | `[OpenApiOperation]`, `[OpenApiRequestBody]`, `[OpenApiResponseWithBody]` |
| DELETE /api/users/{id} | `[OpenApiOperation]`, `[OpenApiParameter]`, `[OpenApiResponseWithoutBody]` |

Access Swagger UI at: `http://localhost:7071/api/swagger/ui`

---

## Notes

1. **Database**: Changed from Entity Framework 6 to Entity Framework Core 9
2. **DI Container**: Changed from Unity to built-in Microsoft.Extensions.DependencyInjection
3. **Hosting**: Changed from IIS-hosted Web API to Azure Functions Isolated Worker
4. **Configuration**: InMemory database used for development; production should use SQL Server or Cosmos DB
5. **Authentication**: Set to Anonymous for development; production should add proper auth
6. **OpenAPI/Swagger**: Added via `Microsoft.Azure.Functions.Worker.Extensions.OpenApi` package
