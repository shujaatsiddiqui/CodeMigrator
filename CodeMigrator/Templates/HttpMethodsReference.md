# HTTP Methods Overview

## Quick Reference

| Operation | Method | Route                      | Body | Success |
|-----------|--------|----------------------------|------|---------|
| Get one   | GET    | /funds/{id}                | No   | 200     |
| Get many  | GET    | /funds                     | No   | 200     |
| Create    | POST   | /funds                     | Yes  | 201     |
| Replace   | PUT    | /funds/{id}                | Yes  | 200     |
| Update    | PATCH  | /funds/{id}                | Yes  | 200     |
| Delete    | DELETE | /funds/{id}                | No   | 204     |
| Action    | POST   | /grants/{id}/approve       | Yes  | 200     |

**Idempotent**: Making the same request multiple times produces the same result.

**Safe**: Does not modify server state.

PATCH can be idempotent depending on implementation.

---

## GET: Retrieve Data

GET is read only. It must never change server state.

### When to Use

- Fetching a single resource by ID
- Fetching a collection of resources
- Searching or filtering data
- Pagination and sorting

### Route Patterns

**Single Resource**

```
GET /api/funds/{id}
```

Give me one specific fund. Mental model: "Show me this fund."

**Collection**

```
GET /api/funds
```

Give me all funds, usually paged. Mental model: "List the funds."

**Nested Resource**

```
GET /api/funds/{fundId}/grants
```

Give me grants that belong to a specific fund. Grants are scoped to a fund in this context. Mental model: "Show me the grants under this fund."

**Search or Filter (Simple)**

```
GET /api/funds?name=scholarship&status=active
```

Search within the collection using query parameters. Filtering is not a new resource, it is just a different view of the same collection. Mental model: "List funds, but only the ones that match these conditions."

**Filtering by Multiple IDs**

```
GET /api/funds?ids=guid1&ids=guid2&ids=guid3
```

or

```
GET /api/funds?ids=guid1,guid2,guid3
```

Use only when the list is small.

**Pagination and Sorting**

```
GET /api/funds?pageNumber=1&pageSize=20&sortBy=name&sortDirection=asc
```

### What NOT to Do

Do NOT send a body in GET:

```
GET /api/funds
Body:
{
  "ids": ["guid1", "guid2"]
}
```

Even though HTTP does not forbid this, many tools ignore the body. This breaks caching, proxies, gateways, and observability.

### When GET Is Not Enough

If filters are large, nested, or complex, use POST for search.

---

## POST: Create, Trigger, Search, or Batch

POST is used when the server is expected to do work. POST may or may not create data, depending on the route.

### When to Use

- Creating a new resource
- Triggering an action or workflow
- Submitting commands
- Performing batch operations
- Performing complex searches

### Route Patterns

**Create a Resource**

```
POST /api/funds
```

Create a new fund. Mental model: "Here is the data. Create something new."

**Action on a Resource**

```
POST /api/grants/{id}/approve
```

Perform an action on an existing grant.

- Not a simple field update
- State changes and side effects may occur
- Operation is not idempotent
- Not PUT or PATCH because approval may trigger workflows, emails, audits, or events

Mental model: "Do something to this grant."

**Batch Operation**

```
POST /api/grants/batch
```

Process multiple grants in one request. Mental model: "Here is a list. Process all of them together."

### POST: Search (Complex Filtering)

Use POST for search when GET query parameters are not enough. This is still a read operation.

```
POST /api/funds/search
```

**When to Use**

- Filtering requires many fields
- Filtering uses collections of IDs
- Filters are nested or structured
- URL length limits may be exceeded

**Example Request Body**

```json
{
  "ids": ["guid1", "guid2"],
  "status": ["active", "pending"],
  "type": "scholarship",
  "minBalance": 1000,
  "createdFrom": "2025-01-01",
  "createdTo": "2025-06-30"
}
```

Mental model: "Search using these rules."

**Important Rules for POST Search**

- Must NOT change server state
- No side effects
- Returns 200 OK
- Acts like a read

### Rules for POST

- POST on a collection usually means create
- POST with a verb means an action
- POST with /search means complex read
- POST may be non idempotent
- POST responses usually return 201 or 200

---

## PATCH: Targeted Update (Explicit Field or Capability)

PATCH is used only for explicit, targeted updates where the route clearly states what is being modified. We do not support generic PATCH endpoints that infer updated fields from request body keys.

### When to Use PATCH

- Exactly one field or capability is being modified
- The update has special validation or side effects
- Explicit intent is required
- The update behaves more like a command than data replacement

### Route Pattern

```
PATCH /api/funds/{id}/{field-or-action}
```

The additional path segment explicitly communicates what is being updated.

### Examples

**Update Email**

```
PATCH /api/funds/123/email
Content-Type: application/json

{
  "value": "new@email.com"
}
```

What the server knows:
- Resource: funds/123
- Field being updated: email
- Payload: new value only
- No ambiguity
- No field detection logic

**Command Style Updates**

```
PATCH /api/users/123/password
PATCH /api/users/123/status
PATCH /api/orders/987/cancel
PATCH /api/accounts/555/lock
```

These routes clearly express intent and may trigger validation, workflows, audits, or events.

### What NOT to Do

Do not use generic PATCH routes.

```
PATCH /api/funds/{id}

{
  "email": "new@email.com",
  "address": { ... }
}
```

This pattern introduces ambiguity and field detection logic, which we intentionally avoid.

### PATCH Summary Rules

- PATCH must include an additional path segment
- One PATCH endpoint updates one field or capability
- PATCH routes should read like commands
- Request body contains only the required value

---

## PUT: Replace Entire Resource (Full Update)

PUT is the only generic update endpoint. PUT replaces the resource using all fields that are allowed to be updated.

### When to Use PUT

- Multiple fields need to be updated together
- The update is data driven, not command driven
- The client can provide the full updatable state

### Route

```
PUT /api/funds/{id}
```

### Required Behavior (Per Team Agreement)

- PUT requests must include all fields that are allowed to be updated
- If a field value should not change, the current value must still be sent
- The server does not infer or preserve missing fields
- Missing fields are treated as intentionally cleared or reset
- This keeps PUT semantics explicit and predictable

### Example

```
PUT /api/funds/123

{
  "name": "Scholarship Fund",
  "email": "current@email.com",
  "address": {
    "line1": "123 Main St",
    "city": "Irvine",
    "state": "CA",
    "zip": "92606"
  },
  "status": "active"
}
```

If a field should remain unchanged, its existing value must be included.

Mental model: "Here is the complete new state of everything I am allowed to update."

### PUT vs PATCH (Final Guidance)

| Scenario                                  | Method                     |
|-------------------------------------------|----------------------------|
| Update one explicit field                 | PATCH with field in route  |
| Update sensitive or command like field    | PATCH with explicit route  |
| Update multiple fields together           | PUT                        |
| Keep existing values                      | Send them explicitly in PUT|
| Workflow or business operation            | PATCH or POST with verb    |

---

## DELETE: Remove Resource

DELETE expresses intent to remove.

### When to Use

- Permanently deleting a resource
- Soft deleting by marking inactive
- Removing relationships

### Route Patterns

**Delete a Resource**

```
DELETE /api/funds/{id}
```

Remove this fund. Whether this is a hard delete or soft delete is a server decision. Mental model: "This fund should no longer exist."

**Remove a Nested Resource or Relationship**

```
DELETE /api/funds/{fundId}/grants/{grantId}
```

Remove a specific grant from a specific fund. The relationship matters and the deletion is scoped to a parent. Mental model: "Remove this grant from this fund."

### DELETE Behavior Rules

- DELETE should be idempotent
- Repeating DELETE should not create new side effects
- Return 204 when successful

---

## Final Rules of Thumb

- Resources live in routes
- Fields live in request bodies
- Actions get verbs
- GET never changes state
- PATCH is explicit and targeted
- PUT is complete and declarative
- PUT never guesses
- If you don't want a field to change, send its current value
- POST is for create, action, batch, or search
- DELETE expresses intent, not implementation
