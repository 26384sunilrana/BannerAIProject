# Tech Stack

## Overview

A polyglot full-stack architecture with TypeScript frontend and C# backend, deployed as containerized microservices on Azure using Kubernetes. This separation allows frontend and backend to scale independently while maintaining type safety and enterprise-grade reliability.

## Languages

**Frontend**: TypeScript
- Type safety catches bugs early in the UI layer
- Excellent tooling and React ecosystem support
- Same language for frontend and backend API integration

**Backend**: C# (.NET)
- Enterprise-grade type system and language features
- Excellent for microservices with strong OOP support
- Native support for async/await patterns
- Rich ecosystem for business logic and data access

## Framework

**Frontend**: Next.js
- Full-stack capabilities (though using for frontend-only in this architecture)
- Server-side rendering and static generation support
- Seamless API route integration for development
- Built-in deployment optimization

**Backend**: ASP.NET Core Web API
- Lightweight, high-performance web framework
- Native support for microservices patterns
- Dependency injection built-in
- Excellent for building RESTful APIs with OpenAPI/Swagger support

## Architecture

**Microservices Pattern**
- Backend organized as independent microservices
- Each microservice deployed separately
- Frontend communicates with backend APIs via HTTP/REST

## Infrastructure & Deployment

**Containerization**: Docker
- Consistent environment from local development to production
- Each service packaged as a container image
- Frontend and each backend microservice in separate containers

**Orchestration**: Kubernetes
- Container orchestration and management
- Auto-scaling based on load
- Service discovery and networking between services
- Rolling updates and rollback capabilities

**Cloud Platform**: Azure
- Managed Kubernetes service (AKS) for orchestration
- Container Registry (ACR) for image storage
- Application Insights for monitoring and observability
- Azure SQL or other data services for persistence

**Development Workflow**
- Local development with Docker Compose for multi-container setup
- Local Kubernetes testing (minikube or Docker Desktop)
- Azure deployment for production and staging environments

## Package Managers

**Frontend**: npm (or pnpm for monorepo efficiency)
- Node Package Manager for Next.js dependencies

**Backend**: NuGet
- .NET package manager for C# dependencies

## Decision Relationships

- **Frontend/Backend Separation**: TypeScript/Next.js frontend can evolve independently from C#/ASP.NET backend
- **Container-First Approach**: Docker enables consistent deployment across environments and simplifies local development
- **Azure Integration**: Kubernetes on Azure (AKS) provides seamless scaling and Azure service integration
- **API Contract**: Frontend and backend communicate via REST APIs with OpenAPI/Swagger documentation

## Notes

- Frontend uses Next.js primarily for its routing and build optimization; backend APIs are separate
- Microservices pattern allows independent scaling and deployment of backend services
- Docker and Kubernetes add operational complexity but provide production reliability
