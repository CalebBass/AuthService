using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthService.Api.Util;
using AuthService.Config;
using AuthService.Data;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace AuthService.Api.UnitTests.Util
{
    public class JwtTokenControllerTests
    {
        private AuthServiceContext _context;
        private IOptionsSnapshot<JwtAttributesConfig> _jwtAttributesConfig;

        private AuthServiceContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AuthServiceContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AuthServiceContext(options);
        }

        private JwtServiceAgent CreateJwtServiceAgent()
        {
            var context = CreateContext();
            _jwtAttributesConfig = A.Fake<IOptionsSnapshot<JwtAttributesConfig>>();

            return new JwtServiceAgent(context, _jwtAttributesConfig);
        }

        [Fact]
        public void JwtServiceAgent_CanCreateClass()
        {
            // Arrange
            var jwtServiceAgent = CreateJwtServiceAgent();

            // Assert
            Assert.NotNull(jwtServiceAgent);
        }


        #region CreateAsymmetricJwtForCovidApi

        [Fact]
        public async Task CreateAsymmetricJwtForCovidApi_If_Successful_ReturnsJwtWithRefreshTokenResponse()
        {
            // Arrange
            var username = "thisIsAUsername";
            var jwtServiceAgent = CreateJwtServiceAgent();

            // Act
            var result = await jwtServiceAgent.CreateAsymmetricJwtForCovidApi(username);

            // Assert
        }


        #endregion CreateAsymmetricJwtForCovidApi
    }
}
