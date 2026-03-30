namespace AegisEInvoicing.Application.Common.Interfaces;

public interface ITotpService
{
    int Generate(string key);
    bool Verify(int otp, string key);
}
