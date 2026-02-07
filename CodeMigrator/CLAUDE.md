# CodeMigrator - Claude Reference

This project migrates legacy .NET applications to Clean Architecture using the SerenitySupport pattern.

## Key Reference Files

When migrating code or scaffolding a new project, always read these files first:

1. `CodeMigrator/Templates/TDDMigrationWorkflow.md` - Complete 5-phase TDD migration process: analyze, generate tests (RED), implement (GREEN), refactor, validate.
2. `CodeMigrator/Templates/CleanArchitectureReference.md` - Full architecture spec, folder structure, layer responsibilities, and dependency rules.
3. `CodeMigrator/Templates/NamingConventions.md` - Naming standards for classes, functions, triggers, DTOs, and files.
4. `CodeMigrator/Templates/CodeTemplates.md` - Copy-paste-ready code templates for every layer: triggers, services, repositories, validators, DTOs, GraphQL, OpenAPI, and audit fields.
5. `CodeMigrator/Templates/HttpMethodsReference.md` - HTTP method conventions for API design: GET, POST, PUT, PATCH, DELETE route patterns, request/response rules, and when to use each method.

## Migration Workflow (TDD Approach)

Follow the 5-phase TDD process defined in `CodeMigrator/Templates/TDDMigrationWorkflow.md`:

1. **Phase 1: Understand & Document** - Analyze legacy code, map execution paths
2. **Phase 2: Generate Tests (RED)** - Write comprehensive tests first (all failing)
3. **Phase 3: Implement Code (GREEN)** - Write minimal code to pass tests
4. **Phase 4: Refactor** - Apply clean architecture without breaking tests
5. **Phase 5: Validate & Document** - Create traceability matrix and coverage report

## Architecture Summary

Target structure uses three projects per solution:

- **SerenitySupport** (Azure Functions entry point) - Triggers, orchestrators, activities
- **SerenitySupport.{Domain}.Library** - Application, Domain, Infrastructure layers
- **SerenitySupport.Common** - Shared constants, enums, helpers, interfaces, models

All dependencies point inward: Triggers -> Application -> Domain <- Infrastructure.
