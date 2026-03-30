# Webhook Configuration

## Configuration

Add the following to your `appsettings.json` or environment variables:

```json
{
  "Webhook": {
    "NotificationUrl": "https://your-webhook-endpoint.com/api/webhook/invoice-transmission"
  }
}
```

## Environment Variables

```bash
Webhook__NotificationUrl=https://your-webhook-endpoint.com/api/webhook/invoice-transmission
```

## Webhook Payload Format

The webhook will send POST requests with the following JSON payload:

```json
{
  "irn": "INV0990-088ED42R-20270920",
  "message": "TRANSMITTING",
  "timestamp": "2024-01-15T10:30:00Z",
  "metadata": {
    "businessId": "guid-here",
    "userId": "guid-here"
  }
}
```

## Message Types

- `TRANSMITTING` - Invoice transmission started
- `TRANSMITTED` - Invoice successfully transmitted to FIRS
- `ACKNOWLEDGED` - FIRS acknowledged receipt
- `FAILED` - Transmission failed

## Retry Logic

- Maximum 3 retry attempts
- Exponential backoff: 1s, 2s, 4s
- 30-second timeout per request
- Client errors (4xx) are not retried
- Server errors (5xx) and network errors are retried

## Security Considerations

- Configure HTTPS endpoints only
- Implement idempotency handling on your webhook receiver
- Log all webhook events for audit trails
- Consider implementing webhook signature verification (can be added if needed)