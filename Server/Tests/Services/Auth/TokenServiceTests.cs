using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Server.Services.Authentication;

namespace Tests.Services.Auth
{
    [TestFixture]
    public class TokenServiceTests
    {
        private TokenService _tokenService;
        private Mock<IConfiguration> _configurationMock;
        private IHttpContextAccessor _contextAccessor;

        [SetUp]
        public void SetUp()
        {
            _configurationMock = new Mock<IConfiguration>();
            _contextAccessor = new HttpContextAccessor();
            _tokenService = new TokenService(_configurationMock.Object, _contextAccessor);
        }

        [Test]
        public void CreateToken_ValidUser_ReturnsValidToken()
        {
            var user = new IdentityUser
            {
                Id = "userId",
                UserName = "testuser",
                Email = "testuser@example.com"
            };

            _configurationMock.Setup(x => x["IssueAudience"]).Returns("tests With Authentication goes &&& comes nowhere");
            _configurationMock.Setup(x => x["IssueSign"]).Returns("!SomethingVeryHardToHackIntoTests!");

            var token = _tokenService.CreateToken(user, "UserRole");

            Assert.IsNotNull(token);
            Assert.IsInstanceOf<JwtSecurityToken>(new JwtSecurityTokenHandler().ReadToken(token));
        }

        [Test]
        public void CreateClaims_ValidUserAndRole_ReturnsListOfClaims()
        {
            var user = new IdentityUser
            {
                Id = "userId",
                UserName = "testuser",
                Email = "testuser@example.com"
            };

            _configurationMock.Setup(x => x["IssueAudience"]).Returns("your_issue_audience");

            var claims = _tokenService.CreateClaims(user, "UserRole");

            Assert.IsNotNull(claims);
            Assert.IsInstanceOf<List<Claim>>(claims);
            Assert.AreEqual(7, claims.Count);
        }
    }
}