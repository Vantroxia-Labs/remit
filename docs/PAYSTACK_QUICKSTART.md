# Paystack Integration Quick Start Guide

## 🚀 Quick Setup (5 Minutes)

### Step 1: Get Your Paystack Keys
1. Sign up at https://paystack.com
2. Go to Settings → API Keys & Webhooks
3. Copy your **Test Secret Key** and **Test Public Key**

### Step 2: Configure Your Application

Update `appsettings.json`:
```json
{
  "Paystack": {
    "SecretKey": "sk_test_YOUR_KEY_HERE",
    "PublicKey": "pk_test_YOUR_KEY_HERE",
    "BaseUrl": "https://api.paystack.co",
    "CallbackUrl": "http://localhost:3000/payment/callback",
    "WebhookSecret": ""
  }
}
```

### Step 3: Set Up Webhook (Optional)
1. Go to Paystack Dashboard → Settings → Webhooks
2. Add URL: `https://your-domain.com/api/v1/payment/webhook`
3. Copy the webhook secret
4. Update `appsettings.json` with the secret

### Step 4: Run Your Application
```bash
cd src/Presentation/AegisEInvoicing.Portal.API
dotnet run
```

Navigate to: `https://localhost:5001/swagger`

## 💳 Test Payment Flow

### 1. Initialize Payment

**Endpoint**: `POST /api/v1/payment/initialize`

```bash
curl -X POST https://localhost:5001/api/v1/payment/initialize \
  -H "Content-Type: application/json" \
  -d '{
    "email": "customer@example.com",
    "amount": 1000000,
    "currency": "NGN",
    "metadata": {
      "planId": "basic-monthly",
      "businessName": "Test Company"
    }
  }'
```

**Response**:
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

### 2. Complete Payment
1. Open the `authorizationUrl` in your browser
2. Use test card: `4084 0840 8408 4081`
3. CVV: `408`, PIN: `0000`, OTP: `123456`
4. Complete the payment

### 3. Verify Payment

**Endpoint**: `GET /api/v1/payment/verify/{reference}`

```bash
curl https://localhost:5001/api/v1/payment/verify/AEGIS-1234567890-ABC123
```

**Response**:
```json
{
  "success": true,
  "data": {
    "reference": "AEGIS-1234567890-ABC123",
    "status": "success",
    "isSuccessful": true,
    "businessId": "guid-here",
    "message": "Payment verified."
  }
}
```

## 📋 Create Subscription Plan

### Create a Monthly Plan

**Endpoint**: `POST /api/v1/subscription/plans`

```bash
curl -X POST https://localhost:5001/api/v1/subscription/plans \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "name": "Basic Monthly Plan",
    "amount": 500000,
    "interval": "monthly",
    "description": "100 invoices per month",
    "currency": "NGN",
    "invoiceLimit": 100
  }'
```

### List All Plans

```bash
curl https://localhost:5001/api/v1/subscription/plans
```

### Get Specific Plan

```bash
curl https://localhost:5001/api/v1/subscription/plans/PLN_abc123
```

## 👤 Subscribe a Customer

**Endpoint**: `POST /api/v1/subscription/subscriptions`

```bash
curl -X POST https://localhost:5001/api/v1/subscription/subscriptions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "customer": "CUS_abc123",
    "plan": "PLN_xyz789",
    "startDate": "2024-02-01T00:00:00Z"
  }'
```

## 🧪 Test Cards

| Card Number          | Result  | Use Case        |
|---------------------|---------|-----------------|
| 4084 0840 8408 4081 | Success | Test successful payments |
| 5531 8866 5214 2950 | Failed  | Test failed payments |
| 5060 6666 6666 6666 666 | Success | Test Verve cards |

**Test Details:**
- CVV: `408` (or any 3 digits)
- PIN: `0000`
- OTP: `123456`
- Expiry: Any future date

## 💰 Amount Conversion

**Important**: Paystack uses kobo (1/100 of Naira)

```csharp
// Convert Naira to Kobo
decimal naira = 10000;        // ₦10,000
long kobo = (long)(naira * 100); // 1,000,000 kobo

// Examples:
₦100    = 10,000 kobo
₦1,000  = 100,000 kobo
₦10,000 = 1,000,000 kobo
```

## 🔌 Frontend Integration Example

### React/TypeScript

```typescript
// utils/paystack.ts
export const initiatePayment = async (
  email: string,
  amount: number, // in Naira
  metadata?: any
) => {
  const response = await fetch('/api/v1/payment/initialize', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      email,
      amount: amount * 100, // Convert to kobo
      currency: 'NGN',
      metadata
    })
  });

  const result = await response.json();
  
  if (result.success) {
    // Redirect to Paystack
    window.location.href = result.data.authorizationUrl;
    return result.data.reference;
  } else {
    throw new Error(result.message);
  }
};

// Verify payment after redirect back
export const verifyPayment = async (reference: string) => {
  const response = await fetch(`/api/v1/payment/verify/${reference}`);
  const result = await response.json();
  
  return result.data;
};

// Usage in component
const handleSubscribe = async () => {
  try {
    const reference = await initiatePayment(
      'user@example.com',
      10000, // ₦10,000
      { planId: 'monthly-plan' }
    );
    
    // Store reference in localStorage or state
    localStorage.setItem('paymentReference', reference);
  } catch (error) {
    console.error('Payment failed:', error);
  }
};

// On callback page
useEffect(() => {
  const reference = localStorage.getItem('paymentReference');
  if (reference) {
    verifyPayment(reference).then(data => {
      if (data.isSuccessful) {
        // Show success message
        console.log('Payment successful!', data.businessId);
      }
    });
  }
}, []);
```

## 🔐 Webhook Handler Example

The webhook is already implemented in `PaymentController`:

```csharp
[HttpPost("webhook")]
[AllowAnonymous]
public async Task<IActionResult> PaystackWebhook()
{
    // Signature validation happens automatically
    // Events handled:
    // - charge.success: Activates business registration
}
```

### Test Webhook Locally

Use ngrok to expose local server:
```bash
ngrok http 5001
```

Then use the ngrok URL in Paystack dashboard:
```
https://abc123.ngrok.io/api/v1/payment/webhook
```

## 📊 Monitoring

### Check Logs

Application logs payment events:
```
[INFO] Initializing Paystack transaction for user@example.com, amount: 1000000
[INFO] Paystack transaction initialized. Reference: AEGIS-1234567890-ABC123
[INFO] Verifying Paystack transaction. Reference: AEGIS-1234567890-ABC123
[INFO] Paystack transaction verified. Status: success
```

### Paystack Dashboard

Monitor payments at:
- **Transactions**: https://dashboard.paystack.com/#/transactions
- **Subscriptions**: https://dashboard.paystack.com/#/subscriptions
- **Customers**: https://dashboard.paystack.com/#/customers

## ⚠️ Common Issues

### Issue: "Invalid signature"
**Solution**: Check webhook secret in configuration

### Issue: "Amount mismatch"
**Solution**: Remember to convert Naira to kobo (multiply by 100)

### Issue: "Payment not verified"
**Solution**: Wait a few seconds after payment, then verify

### Issue: "CORS error"
**Solution**: Add your frontend origin to `appsettings.json` CORS configuration

## 🚀 Go Live Checklist

Before going to production:

- [ ] Replace test keys with live keys
- [ ] Update callback URLs to production domain
- [ ] Configure webhook URL in Paystack dashboard
- [ ] Set webhook secret in production config
- [ ] Enable HTTPS enforcement
- [ ] Test complete payment flow
- [ ] Monitor webhook events
- [ ] Set up payment alerts
- [ ] Document payment procedures

## 📚 Additional Resources

- **Full Documentation**: `docs/PAYSTACK_INTEGRATION.md`
- **Paystack API Docs**: https://paystack.com/docs
- **Paystack Dashboard**: https://dashboard.paystack.com
- **API Reference**: https://localhost:5001/swagger

## 💡 Tips

1. **Always test in test mode first**
2. **Store payment references for tracking**
3. **Log all transactions**
4. **Use webhooks for automatic processing**
5. **Implement retry logic for failed payments**
6. **Send email confirmations to customers**
7. **Monitor Paystack dashboard regularly**

## 🆘 Need Help?

- Check `docs/PAYSTACK_INTEGRATION.md` for detailed guide
- Contact: support@aegisnrs.com
- Paystack Support: support@paystack.com

---

**That's it!** You're now ready to accept payments with Paystack! 🎉
