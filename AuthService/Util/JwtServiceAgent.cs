using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AuthService.Api.Exceptions;
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

        (RefreshTokenValidity TokenValiditiy, string UserName) ValidateRefreshToken(string refreshToken);
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
                await using var transaction = await _context.Database.BeginTransactionAsync();

                await RemovePreviousRefreshToken(username, audience);

                await AddNewRefreshTokenToDatabase(username, audience, refreshToken, refreshExpirationDays);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                saveSuccess = false;
            }

            return saveSuccess ? refreshToken.ToString() : string.Empty;
        }

        private async Task AddNewRefreshTokenToDatabase(string username, string audience, Guid refreshToken, int refreshExpirationDays)
        {
            await _context.JwtRefreshTokens.AddAsync(new JwtRefreshToken
            {
                RefreshToken = refreshToken,
                Username = username,
                Audience = audience,
                ExpirationDateTime = DateTime.Now.AddDays(_jwtAttributes.RefreshTokenDaysUntilExpiration)
            });
        }

        private async Task RemovePreviousRefreshToken(string username, string audience)
        {
            var existingToken =
                _context.JwtRefreshTokens.Where(x =>
                    x.Username == username && x.Audience == audience);

            if (existingToken != null)
            {
                _context.JwtRefreshTokens.RemoveRange(existingToken);
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

        public (RefreshTokenValidity TokenValiditiy, string UserName) ValidateRefreshToken(string refreshToken)
        {
            try
            {
                if (Guid.TryParse(refreshToken, out var result))
                {
                    var rt = _context.JwtRefreshTokens.Where(x => x.RefreshToken == result);

                    if (rt.Any() && rt.Count() == 1)
                    {
                        if (rt.First().ExpirationDateTime > DateTimeOffset.Now)
                        {
                            return (RefreshTokenValidity.Valid, rt.First().Username);
                        }
                        else
                        {
                            return (RefreshTokenValidity.Expired, string.Empty);
                        }
                    }
                    else
                    {
                        //Too many or too little. this can only be one record
                        return (RefreshTokenValidity.Invalid, string.Empty);
                    }
                }
                else
                {
                    throw new ApplicationException("triggering the catch block of this code");
                }
            }
            catch (Exception e)
            {
                throw new NotFoundException("Refresh token not found. Please login again to get a new JWT.");
            }
        }
    }
}

