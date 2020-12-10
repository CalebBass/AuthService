using System;
using System.Collections.Generic;
using System.Text;
using AuthService.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data
{
    public class AuthServiceContext : IdentityDbContext<ApplicationUser>
    {


        public AuthServiceContext(DbContextOptions<AuthServiceContext> options)
            : base(options)
        { }


        public DbSet<JwtRefreshToken> JwtRefreshTokens { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Core Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Core Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
    }
}
