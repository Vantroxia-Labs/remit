using AegisEInvoicing.Paystack.Configuration;
using AegisEInvoicing.Paystack.Interfaces;
using AegisEInvoicing.Paystack.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AegisEInvoicing.Paystack;

public static class DependencyInjection
{
    public static IServiceCollection AddPaystackIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PaystackOptions>(
            configuration.GetSection(PaystackOptions.SectionName));

        var paystackOptions = configuration
            .GetSection(PaystackOptions.SectionName)
            .Get<PaystackOptions>() ?? new PaystackOptions();

        services.AddHttpClient<IPaystackService, PaystackService>(client =>
        {
            client.BaseAddress = new Uri(paystackOptions.BaseUrl);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {paystackOptions.SecretKey}");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
