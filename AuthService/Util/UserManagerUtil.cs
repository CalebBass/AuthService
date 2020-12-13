using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthService.Data;
using AuthService.Data.Models;

namespace AuthService.Api.Util
{
    public interface IUserManagerUtil
    {
        Task<bool> ValidateCredentials(string username, string password);
    }
    public class UserManagerUtil : IUserManagerUtil
    {
        private readonly AuthServiceContext _context;

        public UserManagerUtil(AuthServiceContext context)
        {
            _context = context;
        }


        public async Task<bool> ValidateCredentials(string username, string password)
        {
            throw new NotImplementedException();

            return true;
        }
    }
}
