# Test-Driven Migration Workflow

Follow this Test-Driven Development process for all conversions. Generate comprehensive tests FIRST that capture 100% of the legacy behavior, then write implementation code to pass those tests.

---

## Phase 1: Understand & Document (Analysis)

### Step 1: Parse the Legacy Code
- Identify trigger type and configuration
- List all actions in execution order
- Map all conditions, branches, and loops
- Document all variables and their usage
- Identify error handling scopes and retry policies
- List all external dependencies (SQL, Service Bus, HTTP, etc.)
- Document all data transformations

### Step 2: Map Execution Paths
Create a complete map of every possible path:
- Happy path (everything succeeds)
- Each conditional branch
- Each error scenario
- Timeout scenarios
- Retry scenarios
- Parallel execution paths (if any)

### Step 3: Document Expected Behaviors
For each execution path, document:
- Input data and validation rules
- Expected output
- Side effects (database writes, API calls, messages sent, emails sent)
- Expected error messages
- Expected retry behavior

**Deliverable**: Analysis document with execution path diagram and behavioral specifications

---

## Phase 2: Generate Tests (RED Phase)

### Step 4: Write Unit Tests for Each Action
For every action, write tests that verify:
- Input validation
- Data transformation
- Expected output
- Error handling

### Step 5: Write Integration Tests for Workflow Paths
For each execution path identified in Phase 1:
- Write a test that exercises the entire path
- Mock all external dependencies (SQL, Service Bus, HTTP)
- Assert on final output AND side effects
- Verify the correct sequence of operations

### Step 6: Write Edge Case Tests
- Null/empty inputs
- Boundary values
- Invalid data types
- Concurrent execution (if applicable)
- Timeout scenarios
- Rate limiting scenarios

### Step 7: Write Error Handling Tests
For each error scope:
- Test the error condition
- Verify retry logic (count and delay)
- Verify fallback behavior
- Verify error logging

### Step 8: Write Parallel Execution Tests (if applicable)
- Verify actions execute concurrently
- Verify timing (parallel should be faster than sequential)
- Verify all parallel branches complete
- Verify error in one branch doesn't break others

### Test Naming Convention
```csharp
Given_[Context]_When_[Action]_Then_[ExpectedOutcome]

// Examples:
Given_ValidGrantRequest_When_Processing_Then_GrantIsApproved
Given_InvalidAmount_When_Validating_Then_ReturnsValidationError
Given_DatabaseUnavailable_When_Saving_Then_RetriesThreeTimes
Given_ParallelActions_When_Executing_Then_CompletesUnderTwoSeconds
```

**Deliverable**: Complete test suite (all tests RED/failing) with 100% coverage of legacy behavior

---

## Phase 3: Implement Code (GREEN Phase)

### Step 9: Run Tests (All Should Fail)
- Execute test suite
- Document that all tests fail (expected)

### Step 10: Implement Minimal Code to Pass Tests
For each failing test (in order of dependency):
- Write the minimum code needed to make that test pass
- Run tests after each implementation
- Move to next failing test

### Implementation Order
1. Domain models and DTOs
2. Repository interfaces
3. Service interfaces
4. Command/Query classes
5. Handlers (business logic)
6. Function classes (entry points)
7. Dependency injection setup

### Step 11: Verify All Tests Pass
- Run complete test suite
- All tests must be GREEN
- Fix any failing tests

**Deliverable**: Working implementation with all tests passing

---

## Phase 4: Refactor (REFACTOR Phase)

### Step 12: Apply Clean Architecture
Without breaking tests, refactor code to:
- Proper layer separation (Domain, Application, Infrastructure, Functions)
- SOLID principles
- Remove duplication
- Improve naming
- Add logging

### Step 13: Run Tests Again
- Ensure all tests still pass after refactoring
- No functionality should break

**Deliverable**: Clean, maintainable code with all tests still passing

---

## Phase 5: Validate & Document

### Step 14: Create Traceability Matrix
```
| Legacy Action             | Test Cases                    | Implementation           | Status |
|---------------------------|-------------------------------|--------------------------|--------|
| Trigger: ServiceBus       | Test1, Test2                  | ProcessFunction          | ✓      |
| Condition: Amount > 0     | Test3, Test4, Test5           | ValidateHandler          | ✓      |
| Action: Save to SQL       | Test6, Test7, Test8           | Repository.Create        | ✓      |
| Action: Send Email        | Test9, Test10                 | EmailService.Send        | ✓      |
| Error Scope: Retry        | Test11, Test12, Test13        | RetryPolicy              | ✓      |
```

### Step 15: Document Test Coverage
- Total legacy actions: X
- Total test cases: Y
- Code coverage: Z%
- Any manual steps needed
- Any features not supported

**Deliverable**: Coverage report proving 100% business logic coverage

---

## Quick Reference

| Phase | Name | Key Deliverable |
|-------|------|-----------------|
| 1 | Understand & Document | Analysis document with execution paths |
| 2 | Generate Tests (RED) | Complete test suite - all failing |
| 3 | Implement Code (GREEN) | Minimal code to pass all tests |
| 4 | Refactor | Clean architecture without breaking tests |
| 5 | Validate & Document | Traceability matrix + coverage report |

---

## Post-Conversion Checklist

Use this checklist after migration to verify nothing is missing.

### Project Structure
- [ ] Solution file created (.sln)
- [ ] Function App project created (entry point)
- [ ] Library project created (business logic)
- [ ] Common project created (shared utilities)
- [ ] Test project created
- [ ] All projects reference correct dependencies
- [ ] Target framework is correct (.NET 8/9)

### Domain Layer
- [ ] All entities migrated to Domain/Entities
- [ ] Entity properties match legacy model
- [ ] Repository interfaces defined in Domain/Interfaces
- [ ] Domain events created (if applicable)

### Application Layer
- [ ] Service interfaces created in Application/Interfaces
- [ ] Service implementations in Application/Services
- [ ] All business rules preserved
- [ ] Request DTOs created in Application/DTOs
- [ ] Response DTOs created in Application/DTOs
- [ ] Validators created in Application/Validators
- [ ] All validation rules migrated

### Infrastructure Layer
- [ ] Repository implementations in Infrastructure/Repositories
- [ ] DbContext configured correctly
- [ ] Entity configurations/mappings defined
- [ ] External service clients implemented (if applicable)
- [ ] GraphQL queries/mutations created (if applicable)

### Function Triggers (Entry Points)
- [ ] All endpoints migrated to HTTP triggers
- [ ] Route patterns match legacy API
- [ ] HTTP methods correct (GET, POST, PUT, DELETE)
- [ ] Authorization levels configured
- [ ] Request/response handling implemented
- [ ] Error handling in place

### OpenAPI/Swagger Documentation
- [ ] OpenAPI package installed
- [ ] `[OpenApiOperation]` on all triggers
- [ ] `[OpenApiParameter]` for path/query parameters
- [ ] `[OpenApiRequestBody]` for POST/PUT endpoints
- [ ] `[OpenApiResponseWithBody]` for success responses
- [ ] `[OpenApiResponseWithoutBody]` for error responses
- [ ] Summary and Description provided
- [ ] Tags assigned for grouping

### Dependency Injection
- [ ] DbContext registered
- [ ] Repositories registered
- [ ] Services registered
- [ ] Validators registered
- [ ] Scoped/Singleton lifetimes correct

### Tests
- [ ] Unit tests for all services
- [ ] Unit tests for all validators
- [ ] Integration tests for workflows
- [ ] Edge case tests (null, empty, boundary)
- [ ] Error handling tests
- [ ] All tests passing
- [ ] Test naming follows convention

### Business Rules
- [ ] All validation rules preserved
- [ ] All business logic migrated
- [ ] Error messages match expected behavior
- [ ] Side effects documented and tested

### Configuration
- [ ] local.settings.json configured
- [ ] Connection strings defined
- [ ] Application settings migrated
- [ ] Environment-specific configs handled

### Documentation
- [ ] ANALYSIS.md created (Phase 1 output)
- [ ] TRACEABILITY.md created (Phase 5 output)
- [ ] All endpoints documented
- [ ] Business rules documented
- [ ] Test coverage documented

### Final Verification
- [ ] Solution builds without errors
- [ ] Solution builds without warnings (or warnings documented)
- [ ] All tests pass
- [ ] Function app starts locally
- [ ] Swagger UI accessible
- [ ] All endpoints callable via Swagger
- [ ] Manual smoke test passed

---

## Checklist Summary

```
Total Items: ___
Completed:   ___
Remaining:   ___
Completion:  ___%
```

Sign-off:
- [ ] Developer verified
- [ ] Code reviewed
- [ ] Ready for deployment
