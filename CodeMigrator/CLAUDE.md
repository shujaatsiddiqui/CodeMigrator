# CodeMigrator - Claude Reference

This project migrates legacy .NET applications to Clean Architecture using the SerenitySupport pattern.

## Key Reference Files

When migrating code or scaffolding a new project, always read these files first:

1. `CodeMigrator/Templates/CleanArchitectureReference.md` - Full architecture spec, folder structure, layer responsibilities, and dependency rules.
2. `CodeMigrator/Templates/NamingConventions.md` - Naming standards for classes, functions, triggers, DTOs, and files.
3. `CodeMigrator/Templates/CodeTemplates.md` - Copy-paste-ready code templates for every layer: triggers, services, repositories, validators, DTOs, GraphQL, OpenAPI, and audit fields.

## Migration Workflow

1. **Analyze** the legacy codebase using the appropriate analyzer (webforms, webapi, desktop, logicapp).
2. **Read** the architecture reference to understand the target structure.
3. **Scaffold** the target project using folder structure from the reference.
4. **Generate** code using the templates, adapting to the specific domain.
5. **Generate tests** using the test generators (xunit or nunit).

## Architecture Summary

Target structure uses three projects per solution:

- **SerenitySupport** (Azure Functions entry point) - Triggers, orchestrators, activities
- **SerenitySupport.{Domain}.Library** - Application, Domain, Infrastructure layers
- **SerenitySupport.Common** - Shared constants, enums, helpers, interfaces, models

All dependencies point inward: Triggers -> Application -> Domain <- Infrastructure.
