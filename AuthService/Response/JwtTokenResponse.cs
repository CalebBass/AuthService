using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthService.Api.Response
{
    public class JwtTokenResponse
    {
        public JwtTokenResponse(string accessToken, int minutesUntilExpiration)
        {
            access_token = accessToken;
            expires = DateTimeOffset.Now.AddMinutes(minutesUntilExpiration);
        }
        public string access_token { get; set; }

        public string token_type { get; set; } = "bearer";

        public DateTimeOffset expires { get; set; }
    }
}
