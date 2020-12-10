using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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
        public async Task<ActionResult<JwtWithRefreshTokenResponse>> GetJwtForCovidApi([FromQuery] string username, [FromQuery] string password)
        {
            var user = await _userManager.FindByEmailAsync(username);

            var signInResult = await _singInManager.PasswordSignInAsync(user, password, false, false);

            if (signInResult.Succeeded)
            {
                return await _jwtServiceAgent.CreateAsymmetricJwtForCovidApi(username);
            }

            return StatusCode(500);
        }

//        [HttpGet("Refresh/CovidApi"), MapToApiVersion("1.0")]
//        [ProducesResponseType(typeof(JwtWithRefreshTokenResponse), StatusCodes.Status200OK)]
//        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//        public async Task<ActionResult<JwtWithRefreshTokenResponse>> GetRefreshTokenForCovidApi(string username, string password)
//        {
//            var refreshToken = _refreshTokenUtil.GetRefreshCookieValue(Request);
//
//            if (refreshToken == null) throw new Exception("Refresh token is null");
//
//            var tokenValidationResult = "";
//
//            switch (tokenValidationResult) 
//            {
//                case RefreshTokenValidity.Expired:
//                    Response.Headers.Add("error", "invalid_request");
//                    Response.Headers.Add("error_description", "Expired refresh token");
//                    break;
//
//                case RefreshTokenValidity.Invalid:
//                    Response.Headers.Add("error", "invalid_request");
//                    Response.Headers.Add("error_description", "Invalid refresh token");
//                    break;
//
//                case RefreshTokenValidity.Valid:
//                    var tokenResponse = await _jwtServiceAgent.CreateAsymmetricJwtForCovidApi(username);
//                    _refreshTokenUtil.SaveRefreshTokenToCookie(Response, tokenResponse.refresh_token);
//
//                    return Ok(tokenResponse);
//
//                default:
//                    throw new Exception();
//            }
//
//        }

    }
}
