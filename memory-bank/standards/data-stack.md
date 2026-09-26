# Data Stack

## Overview

A robust, enterprise-grade data access layer using Entity Framework Core with Repository and Unit of Work patterns. Database-first approach with MSSQL for development and Azure SQL Server for production. AutoMapper handles object transformation between domain models and DTOs.

## Database

**Development**: Microsoft SQL Server (MSSQL)
- Local development database
- Full feature parity with Azure SQL Server
- Easy schema management and migration

**Production**: Azure SQL Server
- Managed relational database in Azure
- Automatic backups and disaster recovery
- Scales from thousands to tens of thousands of records
- Integrated with Azure ecosystem
- T-SQL compatibility ensures smooth migration from local MSSQL

**Approach**: Database-First
- Database schema designed first
- Entity Framework Core generates models from database
- Schema changes reflected in models via migrations
- Aligns with careful schema design in early stages

## ORM / Database Client

**Entity Framework Core (EF Core)**
- Version: Latest stable (currently .NET 8+)
- Type-safe database access
- LINQ queries compiled to SQL
- Built-in migration support
- Excellent performance with query optimization

## Data Access Patterns

### Repository Pattern
- Abstracts data access logic behind repository interfaces
- Each entity (Banner, Asset, User, etc.) has a repository
- Repositories handle CRUD operations
- Enables easier unit testing with mock repositories

### Unit of Work Pattern
- Manages database transactions
- Coordinates multiple repositories
- Ensures data consistency across operations
- Commit/Rollback all changes together
- Implementation: Custom UnitOfWork class coordinating repositories

### DTO Mapping with AutoMapper
- Maps between:
  - **Domain Models** (EF entities, used internally)
  - **DTOs** (Data Transfer Objects, sent to API clients)
- Configuration: Fluent AutoMapper profiles per domain area
- Separates internal data model from public API contracts
- Enables API evolution without affecting domain models

## Data Model Areas

**Banners**
- Banner metadata (name, description, created_date, published_date)
- Banner configuration (drag-drop layout, elements)
- Version history (revisions)

**Assets**
- Images, videos, graphics
- Asset metadata and references
- Binary storage (Azure Blob Storage or embedded in DB)

**Users & Permissions**
- User accounts
- Project/banner ownership
- Permission levels

**Audit Trail**
- Change history
- Publish events
- Version control records

## Performance & Scaling

**For Current Scale** (thousands of records):
- MSSQL/Azure SQL Server easily handles this volume
- EF Core with proper indexing is performant
- No need for caching layer initially

**For Future Scale** (10,000+ records):
- Azure SQL Server supports this without issues
- Consider:
  - Database indexing strategy for common queries
  - Read replicas if reporting needs arise
  - Azure Cache for Redis for frequently accessed banner templates

## Decision Relationships

- **DB-First + EF Core**: Natural fit for MSSQL → Azure SQL Server migration
- **Repository + Unit of Work**: Provides clean architecture, testability, transaction management
- **AutoMapper**: Decouples API contracts from domain models, enables independent evolution
- **MSSQL → Azure SQL Server**: Same database engine (T-SQL), smooth migration path

## Notes

- EF Core migrations track schema changes in version control
- Database backups and restore managed by Azure SQL Server or Terraform
- Unit tests use in-memory database or SQLite for fast feedback
- Connection strings differ between development (local MSSQL) and production (Azure SQL Server)
