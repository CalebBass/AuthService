using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AuthService.Api.Response;
using AuthService.Config;
using AuthService.Data;
using AuthService.Data.Models;
using AuthService.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Api.Util
{
    public interface IJwtServiceAgent
    {
        Task<JwtWithRefreshTokenResponse> CreateAsymmetricJwtForCovidApi(string username);
    }

    public class JwtServiceAgent : IJwtServiceAgent
    {
        private readonly AuthServiceContext _context;
        private readonly JwtAttributesConfig _jwtAttributes;

        public JwtServiceAgent(AuthServiceContext context, IOptionsSnapshot<JwtAttributesConfig> jwtAttributes)
        {
            _context = context;
            _jwtAttributes = jwtAttributes.Value;
        }

        public async Task<JwtWithRefreshTokenResponse> CreateAsymmetricJwtForCovidApi(string username)
        {

            var defaultClaims = new List<Claim>
            {
                new Claim(ClaimConstants.UserNameClaimKey, username),
                new Claim(ClaimConstants.IsAdminClaimKey, "false"),
                new Claim(ClaimConstants.IssuerClaimKey, "cbass"),
                new Claim(ClaimConstants.AudienceClaimKey, "PhaseTwo"),
            };

            // get the claims for the user in the DB and add to the defaults

            var jwt = await CreateJwtTokenAsymmetricAsync(defaultClaims, _jwtAttributes.Audience, _jwtAttributes.PrivateRsaKey, _jwtAttributes.ExpirationMinutes);

            var refreshToken = await CreateRefreshToken(username, _jwtAttributes.Audience,
                _jwtAttributes.RefreshTokenDaysUntilExpiration);

            return new JwtWithRefreshTokenResponse(jwt, refreshToken, _jwtAttributes.ExpirationMinutes);
        }

        private async Task<string> CreateRefreshToken(string username, string audience, int refreshExpirationDays)
        {
            var refreshToken = Guid.NewGuid();
            var saveSuccess = true;
            try
            {
                using var transaction = _context.Database.BeginTransaction();

                RemovePreviousRefreshToken(username, audience);

                AddNewRefreshTokenToDatabase(username, audience, refreshToken, refreshExpirationDays);

                await _context.SaveChangesAsync();

                transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                saveSuccess = false;
            }

            return saveSuccess ? refreshToken.ToString() : string.Empty;
        }

        private void AddNewRefreshTokenToDatabase(string username, string audience, Guid refreshToken, int refreshExpirationDays)
        {
            _context.JwtRefreshTokens.AddAsync(new JwtRefreshToken
            {
                RefreshToken = refreshToken,
                Username = username,
                Audience = audience,
                ExpirationDateTime = DateTime.Now.AddDays(_jwtAttributes.RefreshTokenDaysUntilExpiration)
            });
        }

        private async Task RemovePreviousRefreshToken(string username, string audience)
        {
            var existingToken = await _context.JwtRefreshTokens.FirstOrDefaultAsync(x =>
                x.Username.Equals(username, StringComparison.OrdinalIgnoreCase)
                && x.Audience.Equals(audience, StringComparison.OrdinalIgnoreCase));

            if (existingToken == null)
            {
                _context.JwtRefreshTokens.Remove(existingToken);
            }
        }

        private async Task<string> CreateJwtTokenAsymmetricAsync(List<Claim> claims, string audience, string rsaPrivateKey, int jwtExpirationMinutes)
        {

            var privateKey = rsaPrivateKey.ToByteArray();

            using RSA rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(privateKey, out _);

            var credentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
            };

            var token = new JwtSecurityToken(_jwtAttributes.Issuer, audience, claims,
                expires: DateTime.Now.AddMinutes(jwtExpirationMinutes), signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
