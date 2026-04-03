# Paystack Payment and Subscription Integration

This document provides a comprehensive guide to using the Paystack payment and subscription plan integration in the Aegis E-Invoicing platform.

## Table of Contents
1. [Overview](#overview)
2. [Configuration](#configuration)
3. [Payment Flow](#payment-flow)
4. [Subscription Plans](#subscription-plans)
5. [API Endpoints](#api-endpoints)
6. [Webhook Integration](#webhook-integration)
7. [Code Examples](#code-examples)
8. [Testing](#testing)

## Overview

The Paystack integration provides:
- **Payment Processing**: Initialize and verify one-time payments
- **Subscription Plans**: Create and manage recurring subscription plans
- **Subscription Management**: Subscribe customers to plans and manage subscriptions
- **Webhook Support**: Handle payment events from Paystack
- **Secure Validation**: HMAC-SHA512 webhook signature verification

## Configuration

### 1. Update appsettings.json

Ensure your `appsettings.json` has the following Paystack configuration:

```json
{
  "Paystack": {
    "SecretKey": "sk_test_your_secret_key_here",
    "PublicKey": "pk_test_your_public_key_here",
    "BaseUrl": "https://api.paystack.co",
    "CallbackUrl": "https://your-domain.com/payment/callback",
    "WebhookSecret": "your_webhook_secret_here"
  }
}
```

### 2. Environment Variables (Optional)

For production, use environment variables:

```bash
export Paystack__SecretKey="sk_live_your_secret_key"
export Paystack__PublicKey="pk_live_your_public_key"
export Paystack__WebhookSecret="your_webhook_secret"
```

## Payment Flow

### Standard Payment Flow

```
1. User initiates payment
   ↓
2. Backend calls /api/v1/payment/initialize
   ↓
3. Paystack returns authorization URL
   ↓
4. Frontend redirects user to Paystack
   ↓
5. User completes payment on Paystack
   ↓
6. Paystack redirects back to callback URL
   ↓
7. Frontend calls /api/v1/payment/verify/{reference}
   ↓
8. Backend activates subscription/registration
```

### Webhook Flow (Alternative)

```
1. Payment completed on Paystack
   ↓
2. Paystack sends webhook to /api/v1/payment/webhook
   ↓
3. Backend validates signature
   ↓
4. Backend processes event (e.g., charge.success)
   ↓
5. Backend activates subscription/registration
```

## Subscription Plans

### Creating a Plan

Subscription plans define:
- **Amount**: Price in kobo (₦100 = 10,000 kobo)
- **Interval**: Billing frequency (hourly, daily, weekly, monthly, annually)
- **Currency**: NGN (Nigerian Naira)

Example intervals:
- `hourly`: Charge every hour
- `daily`: Charge every day
- `weekly`: Charge every week
- `monthly`: Charge every month
- `annually`: Charge every year

## API Endpoints

### Payment Endpoints

#### Initialize Payment
```http
POST /api/v1/payment/initialize
Content-Type: application/json

{
  "email": "user@example.com",
  "amount": 1000000,  // ₦10,000 in kobo
  "currency": "NGN",
  "callbackUrl": "https://your-app.com/callback",
  "metadata": {
    "pendingRegistrationId": "abc123",
    "planId": "monthly-plan",
    "businessName": "Acme Corp"
  }
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "authorizationUrl": "https://checkout.paystack.com/abc123",
    "accessCode": "abc123xyz",
    "reference": "AEGIS-1234567890-ABC123"
  },
  "message": "Payment initialized successfully."
}
```

#### Verify Payment
```http
GET /api/v1/payment/verify/{reference}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "reference": "AEGIS-1234567890-ABC123",
    "status": "success",
    "isSuccessful": true,
    "businessId": "guid-here",
    "message": "Payment verified. Your account is now active."
  }
}
```

#### Webhook Endpoint
```http
POST /api/v1/payment/webhook
X-Paystack-Signature: <signature>

{
  "event": "charge.success",
  "data": {
    "reference": "AEGIS-1234567890-ABC123",
    "amount": 1000000,
    "status": "success"
  }
}
```

### Subscription Plan Endpoints

#### Create Plan
```http
POST /api/v1/subscription/plans
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "Basic Monthly Plan",
  "amount": 500000,  // ₦5,000 in kobo
  "interval": "monthly",
  "description": "Basic invoicing features",
  "currency": "NGN",
  "invoiceLimit": 100
}
```

#### List Plans
```http
GET /api/v1/subscription/plans?page=1&perPage=50
```

#### Get Plan
```http
GET /api/v1/subscription/plans/{planCode}
```

#### Update Plan
```http
PUT /api/v1/subscription/plans/{planCode}
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "Updated Plan Name",
  "amount": 600000,
  "interval": "monthly"
}
```

### Subscription Management Endpoints

#### Create Subscription
```http
POST /api/v1/subscription/subscriptions
Authorization: Bearer <token>
Content-Type: application/json

{
  "customer": "CUS_abc123",  // Customer code or email
  "plan": "PLN_xyz789",      // Plan code
  "authorization": "AUTH_abc123",  // Optional: for repeat customers
  "startDate": "2024-02-01T00:00:00Z"
}
```

#### List Subscriptions
```http
GET /api/v1/subscription/subscriptions?page=1&perPage=50
Authorization: Bearer <token>
```

#### Get Subscription
```http
GET /api/v1/subscription/subscriptions/{subscriptionCode}
Authorization: Bearer <token>
```

#### Enable Subscription
```http
POST /api/v1/subscription/subscriptions/{code}/enable
Authorization: Bearer <token>
Content-Type: application/json

{
  "emailToken": "token_from_email"
}
```

#### Disable Subscription
```http
POST /api/v1/subscription/subscriptions/{code}/disable
Authorization: Bearer <token>
Content-Type: application/json

{
  "emailToken": "token_from_email"
}
```

## Webhook Integration

### Setting Up Webhooks

1. Log in to your Paystack Dashboard
2. Go to Settings → Webhooks
3. Add webhook URL: `https://your-domain.com/api/v1/payment/webhook`
4. Copy the webhook secret
5. Add secret to configuration

### Supported Events

Currently supported:
- `charge.success`: Payment completed successfully

### Webhook Security

Paystack signs all webhook requests with HMAC-SHA512:

```csharp
X-Paystack-Signature: computed_signature
```

The integration automatically validates signatures to prevent tampering.

## Code Examples

### Frontend Integration (React)

```typescript
// Initialize payment
const initiatePayment = async () => {
  const response = await fetch('/api/v1/payment/initialize', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      email: 'user@example.com',
      amount: 1000000, // ₦10,000
      metadata: {
        pendingRegistrationId: registrationId,
        planId: 'monthly-plan'
      }
    })
  });

  const data = await response.json();
  
  if (data.success) {
    // Redirect to Paystack
    window.location.href = data.data.authorizationUrl;
  }
};

// Verify payment after redirect back
const verifyPayment = async (reference: string) => {
  const response = await fetch(`/api/v1/payment/verify/${reference}`);
  const data = await response.json();
  
  if (data.success && data.data.isSuccessful) {
    // Payment successful, show success message
    console.log('Business activated:', data.data.businessId);
  }
};
```

### Backend Service Usage

```csharp
// Inject service
public class YourService
{
    private readonly IPaystackService _paystackService;

    public YourService(IPaystackService paystackService)
    {
        _paystackService = paystackService;
    }

    // Create a subscription plan
    public async Task CreatePlanAsync()
    {
        var request = new CreatePlanRequest
        {
            Name = "Premium Monthly",
            Amount = 1000000, // ₦10,000
            Interval = "monthly",
            Description = "Premium features",
            Currency = "NGN",
            InvoiceLimit = 500
        };

        var result = await _paystackService.CreatePlanAsync(request);
        
        if (result.Status && result.Data != null)
        {
            Console.WriteLine($"Plan created: {result.Data.PlanCode}");
        }
    }

    // Subscribe customer to plan
    public async Task SubscribeCustomerAsync(string customerCode, string planCode)
    {
        var request = new CreateSubscriptionRequest
        {
            Customer = customerCode,
            Plan = planCode,
            StartDate = DateTimeOffset.UtcNow.AddDays(7)
        };

        var result = await _paystackService.CreateSubscriptionAsync(request);
        
        if (result.Status && result.Data != null)
        {
            Console.WriteLine($"Subscription created: {result.Data.SubscriptionCode}");
            Console.WriteLine($"Next payment: {result.Data.NextPaymentDate}");
        }
    }
}
```

## Testing

### Test Mode

Use Paystack test keys for development:
- **Secret Key**: `sk_test_...`
- **Public Key**: `pk_test_...`

### Test Cards

Paystack provides test cards:

| Card Number         | Type       | Result      |
|---------------------|------------|-------------|
| 4084084084084081    | Visa       | Success     |
| 5060666666666666666 | Verve      | Success     |
| 5531886652142950    | Mastercard | Failed      |

### Amount Conversion

Always convert amounts to kobo:
```csharp
decimal naira = 10000; // ₦10,000
long kobo = (long)(naira * 100); // 1,000,000 kobo
```

### Testing Webhook

Use Paystack's webhook testing tool:
1. Go to Dashboard → Webhooks
2. Click "Test" next to your webhook
3. Select event type (e.g., charge.success)
4. Click "Send Test"

## Security Best Practices

1. **Never expose Secret Key**: Keep it in environment variables
2. **Always validate webhooks**: Check HMAC signature
3. **Use HTTPS**: Never send payments over HTTP
4. **Verify amounts**: Double-check amounts match expectations
5. **Log transactions**: Keep audit trail of all payments
6. **Handle failures**: Implement proper error handling

## Currency Notes

- **NGN**: Nigerian Naira
- **Kobo**: 1/100 of a Naira (like cents)
- **Conversion**: ₦1 = 100 kobo
- **Example**: ₦10,000 = 1,000,000 kobo

## Support

For issues or questions:
- **Paystack Docs**: https://paystack.com/docs
- **Support Email**: support@aegisnrs.com
- **Paystack Support**: support@paystack.com

## License

This integration is part of the Aegis E-Invoicing platform.
