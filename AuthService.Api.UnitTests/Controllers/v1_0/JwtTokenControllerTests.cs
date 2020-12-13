using System;
using System.Threading.Tasks;
using AuthService.Api.Controllers.v1_0;
using AuthService.Api.Response;
using AuthService.Api.Util;
using AuthService.Data;
using AuthService.Data.Models;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace AuthService.Api.UnitTests.Controllers.v1_0
{
    public class JwtTokenControllerTests
    {
        private IJwtServiceAgent _jwtServiceAgent;
        private IRefreshTokenUtil _refreshTokenUtil;
        private SignInManager<ApplicationUser> _signInManager;
        private UserManager<ApplicationUser> _userManager;

        private AuthServiceContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AuthServiceContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AuthServiceContext(options);
        }

        private JwtTokenController CreateJwtTokenController()
        {
            var context = CreateContext();
            _jwtServiceAgent = A.Fake<IJwtServiceAgent>();
            _refreshTokenUtil = A.Fake<IRefreshTokenUtil>();
            _signInManager = A.Fake<SignInManager<ApplicationUser>>();
            _userManager = A.Fake<UserManager<ApplicationUser>>();

            return new JwtTokenController(context, _jwtServiceAgent, _refreshTokenUtil, _signInManager, _userManager);
        }

        [Fact]
        public void JwtServiceAgent_CanCreateClass()
        {
            // Arrange
            var jwtTokenController = CreateJwtTokenController();

            // Assert
            Assert.NotNull(jwtTokenController);
        }


        #region GetJwtForCovidApi

        [Fact]
        public async Task GetJwtForCovidApi_IfSuccessful_ReturnsJwtWithRefreshTokenResponse()
        {
            // Arrange
            var username = "calebbass@test.com";
            var password = "P@ssw0rd";
            var user = new ApplicationUser();
            var jwtTokenController = CreateJwtTokenController();

            A.CallTo(() => _userManager.FindByEmailAsync(username)).Returns(Task.FromResult(user));
            A.CallTo(() => _signInManager.PasswordSignInAsync(A<ApplicationUser>.Ignored, A<string>.Ignored, 
                A<bool>.Ignored, A<bool>.Ignored)).Returns(Task.FromResult(SignInResult.Success));
            // Act
            var result = await jwtTokenController.GetJwtForCovidApi(username, password);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ActionResult<JwtWithRefreshTokenResponse>>(result);
        }

        [Fact]
        public async Task GetJwtForCovidApi_IfSignInFailed_Return401Unauthorized()
        {
            // Arrange
            var username = "calebbass@test.com";
            var password = "P@ssw0rd";
            var user = new ApplicationUser();
            var jwtTokenController = CreateJwtTokenController();
            
            A.CallTo(() => _userManager.FindByEmailAsync(username)).Returns(Task.FromResult(user));
            A.CallTo(() => _signInManager.PasswordSignInAsync(A<ApplicationUser>.Ignored, A<string>.Ignored,
                A<bool>.Ignored, A<bool>.Ignored)).Returns(Task.FromResult(SignInResult.Failed));
            // Act
            var result = await jwtTokenController.GetJwtForCovidApi(username, password);
            var actualResult = result.Result as ObjectResult;

            // Assert
            Assert.Equal(401, actualResult.StatusCode);
            Assert.Equal("invalid_credentials", actualResult.Value);
        }

        [Fact]
        public async Task GetJwtForCovidApi_IfExceptionOccurs_ReturnInternalServerError()
        {
            // Arrange
            var username = "calebbass@test.com";
            var password = "P@ssw0rd";
            var jwtTokenController = CreateJwtTokenController();

            A.CallTo(() => _userManager.FindByEmailAsync(username)).ThrowsAsync(new Exception());
           
            // Act
            var result = await jwtTokenController.GetJwtForCovidApi(username, password);
            var actualResult = result.Result as StatusCodeResult;

            // Assert
            Assert.Equal(500, actualResult.StatusCode);
        }

        #endregion GetJwtForCovidApi


        #region GetRefreshTokenForCovidApi

        [Fact]
        public async Task GetRefreshTokenForCovidApi_IfSuccessful_ReturnsJwtWithRefreshTokenResponse()
        {
            // Arrange
            var username = "calebbass@test.com";
            var password = "P@ssw0rd";
            var jwtTokenController = CreateJwtTokenController();

            A.CallTo(() => _refreshTokenUtil.GetRefreshCookieValue(A<HttpRequest>.Ignored)).Returns(Task.FromResult("refreshToken"));
            A.CallTo(() => _jwtServiceAgent.ValidateRefreshToken(A<string>.Ignored)).Returns((RefreshTokenValidity.Valid, "username"));
            
            // Act
            var result = await jwtTokenController.GetRefreshTokenForCovidApi();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ActionResult<JwtWithRefreshTokenResponse>>(result);
        }

        [Fact]
        public async Task GetRefreshTokenForCovidApi_IfCookieValueIsNull_ReturnInternalServerError()
        {
            // Arrange
            var username = "calebbass@test.com";
            var password = "P@ssw0rd";
            var jwtTokenController = CreateJwtTokenController();

            A.CallTo(() => _refreshTokenUtil.GetRefreshCookieValue(A<HttpRequest>.Ignored)).Returns(Task.FromResult((string) null));
            
            // Act
            var result = await jwtTokenController.GetRefreshTokenForCovidApi();
            var actualResult = result.Result as ObjectResult;

            // Assert
            Assert.Equal(500, actualResult.StatusCode);
        }

        [Fact]
        public async Task GetRefreshTokenForCovidApi_IfRefreshTokenIsExpired_ReturnUnauthorized()
        {
            // Arrange
            var username = "calebbass@test.com";
            var password = "P@ssw0rd";
            var jwtTokenController = CreateJwtTokenController();

            A.CallTo(() => _refreshTokenUtil.GetRefreshCookieValue(A<HttpRequest>.Ignored)).Returns(Task.FromResult("refreshToken"));
            A.CallTo(() => _jwtServiceAgent.ValidateRefreshToken(A<string>.Ignored)).Returns((RefreshTokenValidity.Expired, "username"));

            // Act
            var result = await jwtTokenController.GetRefreshTokenForCovidApi();
            var actualResult = result.Result as ObjectResult;

            // Assert
            Assert.Equal(401, actualResult.StatusCode);
        }

        [Fact]
        public async Task GetRefreshTokenForCovidApi_IfRefreshTokenIsExpired_ReturnBadRequest()
        {
            // Arrange
            var username = "calebbass@test.com";
            var password = "P@ssw0rd";
            var jwtTokenController = CreateJwtTokenController();

            A.CallTo(() => _refreshTokenUtil.GetRefreshCookieValue(A<HttpRequest>.Ignored)).Returns(Task.FromResult("refreshToken"));
            A.CallTo(() => _jwtServiceAgent.ValidateRefreshToken(A<string>.Ignored)).Returns((RefreshTokenValidity.Invalid, "username"));

            // Act
            var result = await jwtTokenController.GetRefreshTokenForCovidApi();
            var actualResult = result.Result as ObjectResult;

            // Assert
            Assert.Equal(400, actualResult.StatusCode);
        }

       
        #endregion GetRefreshTokenForCovidApi

    }
}
