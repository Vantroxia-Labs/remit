namespace AegisEInvoicing.Domain.Enums;

[Flags]
public enum NotificationChannel
{
    None = 0,
    InApp = 1,
    Email = 2,
    SMS = 4,
    All = InApp | Email | SMS
}