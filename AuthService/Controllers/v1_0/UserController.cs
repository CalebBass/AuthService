using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AuthService.Api.Request;
using AuthService.Api.Util;
using AuthService.Data;
using AuthService.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
        private readonly SignInManager<ApplicationUser> _signInManager;

        public UserController(AuthServiceContext context, UserManager<ApplicationUser> userManager, 
            IJwtServiceAgent jwtServiceAgent, SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _jwtServiceAgent = jwtServiceAgent;
            _signInManager = signInManager;
        }

        [HttpPost, MapToApiVersion("1.0")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateUser(
            [FromHeader(Name = RequestHeaderConstants.UserName)][Required] string username,
            [FromHeader(Name = RequestHeaderConstants.Password)][Required] string password,
            [FromHeader(Name = RequestHeaderConstants.PhoneNumber)][Required] string phoneNumber)
        {
            var newUser = new ApplicationUser { Email = username, UserName = username, PhoneNumber = phoneNumber };
            var createNewUserResult = await _userManager.CreateAsync(newUser, password);

            if (createNewUserResult.Succeeded)
            {
                return Ok();
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }


        /// <summary>
        /// Update user details
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [HttpPut, MapToApiVersion("1.0")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUserInfo([FromBody] UpdateUserRequest user,
            [FromHeader(Name = RequestHeaderConstants.Password)][Required] string password)
        {
            try
            {
               var oldUserDetails = await _userManager.FindByNameAsync(user.UserName);

                if (oldUserDetails == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }

                var signInResult = await _signInManager.PasswordSignInAsync(oldUserDetails, password, false, false);

                if (!signInResult.Succeeded)
                {
                    BadRequest("Invalid credentials");
                }

                oldUserDetails.NormalizedEmail = user.Email.ToUpper();
                _context.Entry(oldUserDetails).CurrentValues.SetValues(user);
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                throw;
            }
        }


        [HttpDelete, MapToApiVersion("1.0")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(
            [FromHeader(Name = RequestHeaderConstants.UserName)][Required] string username,
            [FromHeader(Name = RequestHeaderConstants.Password)][Required] string password)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(username);

                if (user == null)
                {
                    return NotFound("User not found");
                }

                var credentialCheckResult = await _signInManager.PasswordSignInAsync(user, password, false, false);

                if (!credentialCheckResult.Succeeded)
                {
                    BadRequest("Invalid credentials");
                }

                var deleteResult = await _userManager.DeleteAsync(user);

                if (!deleteResult.Succeeded)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something went wrong" });
                }

                return Ok("User was deleted");
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
