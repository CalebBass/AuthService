using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AuthService.Data.Models
{
    public class JwtRefreshToken
    {
        [Key]
        public Guid RefreshToken { get; set; }

        public string Username { get; set; }

        public string Audience { get; set; }

        public DateTime ExpirationDateTime { get; set; }
    }
}
