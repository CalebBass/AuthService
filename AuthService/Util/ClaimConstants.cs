using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthService.Util
{
    public static class ClaimConstants
    {
        public static readonly string UserNameClaimKey = "un";
        public static readonly string IsAdminClaimKey = "admin";
        public static readonly string NameClaimKey = "name";
        public static readonly string IssuerClaimKey = "iss";
        public static readonly string AudienceClaimKey = "aud";
        public static readonly string ExpirationClaimKey = "exp";
    }
}
