# LastCall Motor Auctions

Capstone Project - CPRO 2901  
Red Deer Polytechnic

## Project Description
A secure, nationwide automotive auction platform that modernizes traditional vehicle auctions with real-time bidding, professional inspections, and nationwide shipping.

## Technology Stack

### Backend
- ASP.NET Core Web API (C#)
- Microsoft SQL Server
- Entity Framework Core

### Frontend
- JavaScript
- HTML5
- CSS3

## Team Members
- Chase Petrowsky (359601)
- Ahmed Said (373728)
- Naftali Kibet (368953)
- Nazarii Goncharuk (367506)

## Getting Started

### Prerequisites
- .NET 8.0 SDK or higher
- Microsoft SQL Server
- Git

### Setup
1. Clone the repository
2. Restore dependencies: `dotnet restore`
3. Build the project: `dotnet build`
4. Run the project: `dotnet run`

## Project Status
Initial setup - Backend template created

## Backend Setup Checklist

### Core Foundation
- [ ] Design and implement core domain models (User, Vehicle, Auction, Bid, Inspection, Shipping, etc.)
- [ ] Configure DbContext with DbSet properties for all entities and set up relationships/foreign keys
- [ ] Create initial database migration and update database schema
- [x] Set up authentication and authorization (JWT tokens, Identity framework, or custom auth)
- [x] Implement repository pattern or service layer for data access abstraction

### API Development
- [ ] Create API controllers for core entities (Users, Vehicles, Auctions, Bids)
- [ ] Add input validation
- [ ] Create DTOs (Data Transfer Objects) for API requests/responses
- [x] Set up API versioning and Swagger/OpenAPI documentation

### Security & Configuration
- [x] Configure CORS policy for frontend integration
- [x] Add appsettings.Development.json with development connection string
- [x] Set up dependency injection for services and repositories

### Quality & Operations
- [x] Implement global error handling middleware and exception handling
- [x] Implement logging configuration (Serilog or built-in logging)
- [x] Add health check endpoints for monitoring