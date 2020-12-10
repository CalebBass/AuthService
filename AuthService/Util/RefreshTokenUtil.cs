using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthService.Config;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AuthService.Api.Util
{
    public interface IRefreshTokenUtil
    {
        Task SaveRefreshTokenToCookie(HttpResponse response, string refreshToken);
        Task<string> GetRefreshCookieValue(HttpRequest request);

    }


    public class RefreshTokenUtil : IRefreshTokenUtil
    {
        private readonly JwtAttributesConfig _jwtAttributesConfig;
        private readonly IDataProtectionProvider _dataProtectionProvider;

        public RefreshTokenUtil(IOptionsSnapshot<JwtAttributesConfig> jwtAttributesConfig,
            IDataProtectionProvider dataProtectionProvider)
        {
            _jwtAttributesConfig = jwtAttributesConfig.Value;
            _dataProtectionProvider = dataProtectionProvider;
        }


        public async Task SaveRefreshTokenToCookie(HttpResponse response, string refreshToken)
        {
            var protectedToken =
                _dataProtectionProvider.CreateProtector("ProtectTheRefreshToken").Protect(refreshToken);

            AppendRefreshTokenCookie(response, protectedToken, _jwtAttributesConfig.RefreshCookieName);
        }

        public async Task<string> GetRefreshCookieValue(HttpRequest request)
        {
            var refreshToken = request.Cookies[_jwtAttributesConfig.RefreshCookieName];
            var cookieVal = _dataProtectionProvider.CreateProtector("ProtectTheRefreshToken").Unprotect(refreshToken);

            return cookieVal;
        }

        private void AppendRefreshTokenCookie(HttpResponse response, string refreshToken, string cookieKey)
        {
            response.Cookies.Append(cookieKey, refreshToken, 
                new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTime.Now.AddDays(_jwtAttributesConfig.RefreshTokenDaysUntilExpiration),
                    IsEssential = true
                });
        }
    }
}
