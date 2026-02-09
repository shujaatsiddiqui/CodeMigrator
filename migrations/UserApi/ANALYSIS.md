# Phase 1: Legacy API Analysis

## Source: .NET Framework 4.8 Web API
Repository: https://github.com/kaunglvlv/net48-web-api-example

---

## 1. Project Structure

```
WebApiExample.WebApp           -> Entry point (MVC + Web API)
WebApiExample.DataStore        -> Entity Framework DbContext
WebApiExample.Common           -> Shared interfaces
WebApiExample.WebApp.Tests     -> Unit tests (xUnit)
```

---

## 2. API Endpoints

| Method | Route              | Action           | Description                  |
|--------|-------------------|------------------|------------------------------|
| GET    | /api/users        | Get()            | Get all users                |
| GET    | /api/users/{id}   | Get(Guid id)     | Get user by ID               |
| POST   | /api/users        | Post(NewUser)    | Create new user              |
| DELETE | /api/users/{id}   | Delete(Guid id)  | Delete user by ID            |

---

## 3. Domain Model

### User Entity
```csharp
public class User : IRootEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}
```

### NewUser Request DTO
```csharp
public class NewUser
{
    [Required] public string Name { get; set; }
    [Required] public int Age { get; set; }
}
```

---

## 4. Business Rules

| Rule | Description |
|------|-------------|
| BR-001 | Name is required for user creation |
| BR-002 | Age is required for user creation |
| BR-003 | Username "admin" is banned |
| BR-004 | Username "sa" is banned |
| BR-005 | Delete silently succeeds if user not found |

---

## 5. Execution Paths

### GET /api/users (Get All Users)
```
[Happy Path]
Request -> UserService.GetAllUsersAsync() -> Return 200 OK with user list

[Error Path]
Request -> Database Exception -> Return 500 Internal Server Error
```

### GET /api/users/{id} (Get User by ID)
```
[Happy Path]
Request -> UserService.GetUserAsync(id) -> User found -> Return 200 OK with user

[Not Found Path]
Request -> UserService.GetUserAsync(id) -> User is null -> Return 404 Not Found

[Error Path]
Request -> Database Exception -> Return 500 Internal Server Error
```

### POST /api/users (Create User)
```
[Happy Path]
Request -> Validate model -> UserService.AddUserAsync() -> Return 201 Created with user

[Validation Error Path]
Request -> ModelState invalid -> Return 400 Bad Request "Name and age are required"

[Banned Name Path]
Request -> UserService.AddUserAsync() -> Name is banned -> Return 400 Bad Request "{name} is not allowed"

[Error Path]
Request -> Database Exception -> Return 500 Internal Server Error
```

### DELETE /api/users/{id} (Delete User)
```
[Happy Path]
Request -> UserService.RemoveAsync(id) -> User found -> Delete -> Return 204 No Content

[Not Found Path]
Request -> UserService.RemoveAsync(id) -> User not found -> Return 204 No Content (silent)

[Error Path]
Request -> Database Exception -> Return 500 Internal Server Error
```

---

## 6. Dependencies

| Dependency | Purpose |
|------------|---------|
| Entity Framework 6 | ORM / Data Access |
| Unity | Dependency Injection |
| SQL Server | Database |

---

## 7. External Services

None - This is a simple CRUD API with database access only.

---

## 8. Test Coverage from Legacy

Existing tests in `UserServiceTest.cs`:
- AddNewUserAsync_Adds_A_New_User
- AddNewUserAsync_Throws_When_Using_Banned_Names (admin, sa)
- AddNewUserAsync_Db_Exception_Bubbles_Up
- GetAllUsersAsync_Returns_Users
- GetAllUsersAsync_Db_Exception_Bubbles_Up
- GetUserAsync_Returns_User
- GetUserAsync_Returns_Null_When_Not_Found
- GetUserAsyn_Db_Exception_Bubbles_Up
- RemoveAsync_Removes_Existing_User
- RemoveAsync_Returns_If_User_Not_Found
- RemoveAsync_Get_User_Db_Exception_Bubbles_Up
- RemoveAsync_Commit_Db_Exception_Bubbles_Up

---

## 9. Migration Target

Convert to Azure Functions with:
- .NET 8 Isolated Worker
- Clean Architecture (SerenitySupport pattern)
- Entity Framework Core 8
- xUnit tests with Moq
