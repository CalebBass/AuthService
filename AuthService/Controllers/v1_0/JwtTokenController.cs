using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AuthService.Api.Response;
using AuthService.Api.Util;
using AuthService.Data;
using AuthService.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Api.Controllers.v1_0
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class JwtTokenController : ControllerBase
    {
        private readonly AuthServiceContext _context;
        private readonly IJwtServiceAgent _jwtServiceAgent;
        private readonly IRefreshTokenUtil _refreshTokenUtil;
        private readonly SignInManager<ApplicationUser> _singInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public JwtTokenController(AuthServiceContext context, IJwtServiceAgent jwtServiceAgent, IRefreshTokenUtil refreshTokenUtil, 
            SignInManager<ApplicationUser> singInManager, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _jwtServiceAgent = jwtServiceAgent;
            _refreshTokenUtil = refreshTokenUtil;
            _singInManager = singInManager;
            _userManager = userManager;
        }

        [HttpGet("Issue/CovidApi"), MapToApiVersion("1.0")]
        [ProducesResponseType(typeof(JwtWithRefreshTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType( StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<JwtWithRefreshTokenResponse>> GetJwtForCovidApi(string username, string password)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(username);

                var signInResult = await _singInManager.PasswordSignInAsync(user, password, false, false);

                if (signInResult.Succeeded)
                {
                    var tokenResponse = await _jwtServiceAgent.CreateAsymmetricJwtForCovidApi(username);
                    await _refreshTokenUtil.SaveRefreshTokenToCookie(Response, tokenResponse.refresh_token);

                    return tokenResponse;
                }
                else
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, "invalid_credentials");
                }
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("Refresh/CovidApi"), MapToApiVersion("1.0")]
        [ProducesResponseType(typeof(JwtWithRefreshTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(JwtWithRefreshTokenResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(JwtWithRefreshTokenResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(JwtWithRefreshTokenResponse), StatusCodes.Status500InternalServerError)]


        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<JwtWithRefreshTokenResponse>> GetRefreshTokenForCovidApi()
        {
            try
            {
                var refreshTokenValue = await _refreshTokenUtil.GetRefreshCookieValue(Request);

                if (refreshTokenValue == null)
                {
                    throw new UnauthorizedAccessException("User does not have a refresh token cookie.");
                }

                var tokenValidationResult = _jwtServiceAgent.ValidateRefreshToken(refreshTokenValue);

                switch (tokenValidationResult.TokenValiditiy)
                {
                    case RefreshTokenValidity.Expired:
                        return Unauthorized("Expired refresh token");

                    case RefreshTokenValidity.Invalid:
                        return BadRequest("Invalid refresh token");

                    case RefreshTokenValidity.Valid:
                        var tokenResponse =
                            await _jwtServiceAgent.CreateAsymmetricJwtForCovidApi(tokenValidationResult.UserName);
                        _refreshTokenUtil.SaveRefreshTokenToCookie(Response, tokenResponse.refresh_token);

                        await _refreshTokenUtil.SaveRefreshTokenToCookie(Response, tokenResponse.refresh_token);

                        return Ok(tokenResponse);

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (UnauthorizedAccessException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
            catch (ArgumentOutOfRangeException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong");

            }

        }

    }
}
