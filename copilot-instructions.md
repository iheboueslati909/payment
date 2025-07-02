# CoPilot Instructions for Payment Gateway Project

## Context
You are assisting in building a **centralized payment gateway** designed to serve multiple external applications (tenants). The gateway is responsible for processing payments (starting with Stripe), ensuring data isolation across apps, and supporting reliability and security.

We are using:
- **.NET 9**
- **PostgreSQL**
- **Vertical Slice Architecture**
- **Dependency Injection**
- **RabbitMQ** (via MassTransit)
- **Transactional Outbox pattern**
- **Azure** for deployment
- **GitHub Actions** for CI/CD

### Key Business Concepts

- `AppId`: Identifies the external app (merchant).
- `UserId`: Identifies the end customer **within** an app.
- Each payment belongs to one (appId, userId) pair.
- We must isolate data per app.

---

## Coding Guidelines

### ✅ Do:
- Use **Vertical Slice Architecture**: Keep features self-contained in folders (e.g., `Features/Payments/CreatePayment.cs`)
- Use **feature-first folder structure**.
- Use minimal Command/Handler pattern.
- Use **Dependency Injection** via built-in .NET 9 DI container.
- Handle **idempotency**: support idempotency keys from clients and avoid duplicate processing.
- Validate **JWTs issued by external apps**, and extract `appId` from token claims.
- Implement **role-based access**:
  - Admin: All access.
  - Merchant: Limited to their own transactions.
- Use **RabbitMQ** for async events (e.g., refund processing, notifications).
- Use **Transaction Outbox pattern** for publishing events after DB commits.
- Include webhook delivery and **signature verification** (Stripe style).

### ❌ Do Not:
- Use DDD, CQRS, or over-engineered layers.
- Use a shared DB schema between apps. Use `appId` to logically isolate data.
- Hardcode Stripe logic — abstract it behind an interface even if there's only one provider now.

---

## Initial Features to Implement

1. **[POST] /payments**
   - Accepts: amount, currency, payment method (Stripe), idempotency key, userId, appId (via JWT)
   - Validates JWT, userId, appId
   - Charges via Stripe
   - Persists transaction
   - Publishes `PaymentProcessedEvent` to RabbitMQ
   - Returns payment result

2. **[POST] /payments/refund**
   - Accepts: paymentId
   - Validates ownership via appId
   - Sends refund command to RabbitMQ queue
   - Refund handled asynchronously

3. **[POST] /webhooks/stripe**
   - Verifies Stripe signature
   - Updates transaction status

4. **Webhook Management (internal admin-only)**
   - Register or update app webhook URLs and secrets

5. **Idempotency Table**
   - Stores processed keys with appId/userId/paymentId
   - Expiration cleanup in background

---

## Project structure
PaymentGateway/
│
├── Api/
│   ├── Program.cs
│   ├── Startup.cs
│   └── Middlewares/
│       └── ExceptionHandlingMiddleware.cs
│
├── Features/
│   ├── Payments/
│   │   ├── Create/
│   │   │   ├── CreatePaymentCommand.cs
│   │   │   ├── CreatePaymentHandler.cs
│   │   │   └── CreatePaymentRequest.cs
│   │   ├── Refund/
│   │   │   ├── RequestRefundCommand.cs
│   │   │   ├── RequestRefundHandler.cs
│   │   │   └── RequestRefundRequest.cs
│   │   └── Models/
│   │       └── Payment.cs
│   │
│   ├── Webhooks/
│   │   ├── StripeWebhookHandler.cs
│   │   └── AppWebhookDispatcher.cs
│   │
│   ├── Apps/
│   │   ├── RegisterAppCommand.cs
│   │   ├── App.cs
│   │   └── WebhookConfig.cs
│   │
│   └── Admin/
│       ├── GetAllAppsHandler.cs
│       └── RetryWebhookCommand.cs
│
├── Infrastructure/
│   ├── Auth/
│   │   └── JwtValidator.cs
│   ├── Database/
│   │   ├── AppDbContext.cs
│   │   ├── EntityConfigurations/
│   │   └── Migrations/
│   ├── PaymentProviders/
│   │   ├── Stripe/
│   │   │   └── StripePaymentProvider.cs
│   │   └── IPaymentProvider.cs
│   ├── Messaging/
│   │   ├── RabbitMqPublisher.cs
│   │   ├── Consumers/
│   │   │   └── RefundRequestedConsumer.cs
│   │   └── Contracts/
│   │       └── PaymentProcessedEvent.cs
│   ├── Outbox/
│   │   ├── OutboxMessage.cs
│   │   ├── OutboxProcessor.cs
│   │   └── OutboxPublisherService.cs
│   ├── Webhooks/
│   │   └── WebhookSender.cs
│   └── Logging/
│       └── SerilogConfig.cs (or AppInsights etc.)
│
├── Common/
│   ├── Interfaces/
│   │   ├── ICommand.cs
│   │   ├── ICommandHandler.cs
│   │   └── IScopedService.cs
│   ├── Errors/
│   │   └── ProblemDetailsFactory.cs
│   ├── Extensions/
│   │   └── HttpContextExtensions.cs
│   ├── Enums/
│   │   └── PaymentStatus.cs
│   └── Constants/
│       └── Roles.cs, Claims.cs
│
├── BackgroundWorkers/
│   ├── OutboxWorker.cs
│   └── WebhookRetryWorker.cs
│
├── Tests/
│   ├── Payments/
│   └── Webhooks/
│
├── appsettings.json
├── docker-compose.yml
└── copilot-instructions.md
