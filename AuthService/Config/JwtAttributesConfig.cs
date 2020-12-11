using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthService.Config
{
    public class JwtAttributesConfig
    {
        public string SymmetricKey { get; set; } = string.Empty;

        public string Issuer { get; set; } = string.Empty;

        public int RefreshTokenDaysUntilExpiration { get; set; } 

        public int ExpirationMinutesMatrix { get; set; }

        public string Audience { get; set; } = string.Empty;

        public int ExpirationMinutes { get; set; }

        public string RefreshTokenCookieName { get; set; } = string.Empty;

        public string PrivateRsaKey { get; set; } = string.Empty;

        public string PublicRsaKey { get; set; } = string.Empty;
    }
}
