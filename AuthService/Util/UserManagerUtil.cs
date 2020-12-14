using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthService.Api.Exceptions;
using AuthService.Data;
using AuthService.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Api.Util
{
    public interface IUserManagerUtil
    {
        Task<bool> ValidateCredentials(string username, string password);
    }
    public class UserManagerUtil : IUserManagerUtil
    {
        private readonly AuthServiceContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserManagerUtil(AuthServiceContext context, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public async Task<bool> ValidateCredentials(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
            {
                throw new NotFoundException("Username does not exist");
            }
            var signInResult = await _signInManager.PasswordSignInAsync(user, password, false, false);

            return signInResult.Succeeded;
        }
    }
}
