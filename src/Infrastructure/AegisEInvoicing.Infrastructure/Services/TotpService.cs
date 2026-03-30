using AspNetCore.Totp;
using AspNetCore.Totp.Interface;
using AegisEInvoicing.Application.Common.Interfaces;

namespace AegisEInvoicing.Infrastructure.Services
{
    public class TotpService : ITotpService
    {
        private readonly ITotpGenerator _totpGenerator;
        private const int ToleranceSeconds = 300;

        public TotpService(ITotpGenerator totpGenerator)
        {
            _totpGenerator = totpGenerator;
        }
        public int Generate(string key)
        {
            return _totpGenerator.Generate(key);
        }

        public bool Verify(int otp, string key)
        {
            return new TotpValidator(_totpGenerator).Validate(key, otp, ToleranceSeconds);
        }
    }
}
