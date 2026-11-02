# Personal Finance Platform

Microservice-based personal finance app: **.NET 9 / ASP.NET Core**, **EF Core + SQL Server**, **Angular 21** (standalone, signals, reactive forms).

## Repository layout

```
PersonalFinancePlatform/
├── PersonalFinancePlatform.sln
├── Directory.Build.props          # TFM, nullable, analyzers (SonarAnalyzer.CSharp)
├── Directory.Packages.props       # central NuGet versions
├── docker-compose.yml             # SQL Server + RabbitMQ for local dev
├── src/
│   ├── BuildingBlocks/
│   │   ├── PFP.Contracts/         # enums, integration events, AppException types (no framework deps)
│   │   └── PFP.BuildingBlocks/    # Serilog setup, correlation-id + global exception middleware
│   ├── Services/                  # one folder per service, one database/schema per service
│   │   ├── Auth/PFP.Auth.Api              :5001  (stub)
│   │   ├── Expense/                       :5002
│   │   │   ├── PFP.Expense.Domain         entity + invariants
│   │   │   ├── PFP.Expense.Infrastructure ExpenseDbContext, EF configurations
│   │   │   └── PFP.Expense.Api
│   │   ├── Budget/                        :5003
│   │   │   ├── PFP.Budget.Domain / .Infrastructure / .Api
│   │   └── Summary/PFP.Summary.Api        :5004  (stub)
│   └── web/finance-web/           # Angular app (proxy.conf.json routes /api/* to the services)
└── tests/
    ├── PFP.BuildingBlocks.Tests / PFP.Expense.Tests / PFP.Budget.Tests   (xUnit, NSubstitute, FluentAssertions)
    └── load/k6/summary.js
```

Dependencies point inward: `Api -> Infrastructure -> Domain -> Contracts`. Services never share a database; they
communicate through REST (Summary -> Expense/Budget) and events (`ExpenseRecorded`, `BudgetThresholdBreached`)
defined in `PFP.Contracts`.

## Getting started

```powershell
copy .env.example .env          # set MSSQL_SA_PASSWORD / RABBITMQ_PASSWORD
docker compose up -d            # or use SQL Server LocalDB (the Development connection strings default to it)

dotnet test                     # backend tests
dotnet run --project src/Services/Expense/PFP.Expense.Api

cd src/web/finance-web
npm install --legacy-peer-deps  # workaround for an npm 10 arborist bug seen with Angular 21 + vitest
npm start                       # http://localhost:4200, proxies /api/* to :5001-:5004
npm test
```

## Configuration and secrets

Layering (last wins): `appsettings.json` -> `appsettings.{Environment}.json` -> user-secrets -> environment variables
(`ConnectionStrings__ExpenseDb`, `Serilog__MinimumLevel__Default`, ...). Committed files contain no secrets; the
Development connection strings use Windows auth on LocalDB. For non-dev environments inject secrets via environment
variables or Azure Key Vault (`builder.Configuration.AddAzureKeyVault(...)`) — planned alongside the Auth service.

## Conventions

- Logging: Serilog compact JSON to console; every line carries `Service`, `Environment`, `CorrelationId`.
  The `X-Correlation-ID` header is accepted (sanitised) or generated, and echoed on the response.
- Errors: RFC 7807 `application/problem+json` with `errorCode`, `traceId`, `correlationId`; 500s never expose messages or stack traces.
- Test stack on the frontend is Vitest (Angular 21 default, Jest-compatible API).
- `FluentAssertions` is pinned to 7.x (8+ is commercially licensed).
- Target framework is a single property in `Directory.Build.props` (`net9.0`); switch to `net10.0` (LTS) once that SDK is installed.

## Roadmap

1. Auth service: registration, BCrypt hashing, JWT issuance, roles; shared JWT bearer setup in BuildingBlocks.
2. Expense/Budget endpoints with FluentValidation + MediatR handlers; EF migrations (`dotnet ef migrations add Initial`).
3. MassTransit/RabbitMQ: publish `ExpenseRecorded`, Budget consumer evaluates thresholds and publishes `BudgetThresholdBreached`.
4. Summary aggregation (Refit clients) and an income source for the dashboard's "income vs expenses".
5. Angular: auth flow, interceptors, expense and budget forms.
