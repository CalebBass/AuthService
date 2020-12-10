using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthService.Api.Util;
using AuthService.Data;
using AuthService.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Api.Controllers.v1_0
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AuthServiceContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtServiceAgent _jwtServiceAgent;

        public UserController(AuthServiceContext context, UserManager<ApplicationUser> userManager, IJwtServiceAgent jwtServiceAgent)
        {
            _context = context;
            _userManager = userManager;
            _jwtServiceAgent = jwtServiceAgent;
        }

        [HttpPost("Create"), MapToApiVersion("1.0")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateUser(string username, string password)
        {

            var newUser = new ApplicationUser {Email = username, UserName = username};
            var createNewUserResult = await _userManager.CreateAsync(newUser, password);

            if (createNewUserResult.Succeeded)
            {
                return Ok();
            }

            return StatusCode(500);
        }
    }
}
