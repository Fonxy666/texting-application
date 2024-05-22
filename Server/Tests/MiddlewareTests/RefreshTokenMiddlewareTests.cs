using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Server.Middlewares;
using Server.Model;
using Server.Services.Authentication;
using Server.Services.Cookie;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.MiddlewareTests;

public class RefreshTokenMiddlewareTests
{
    private readonly Mock<ITokenService> _mockTokenService = new();
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager = MockUserManager.Create();
    private readonly Mock<ICookieService> _mockCookieService = new();
    private readonly RequestDelegate _next = new Mock<RequestDelegate>().Object;

    [Fact]
    public async Task Invoke_TokenNotExpired_ShouldCallNext()
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Cookies = new MockCookieCollection(
                    "Authorization", GenerateJwtToken(DateTime.UtcNow.AddSeconds(1)), 
                    "RefreshToken", "invalid-refresh-token", 
                    "UserId", "user-id", 
                    "RememberMe", "True"
                )
            }
        };

        var user = new ApplicationUser("example.url");
        user.SetRefreshToken("valid-refresh-token");
        _mockUserManager.Setup(um => um.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);

        var middleware = new RefreshTokenMiddleware(_next);

        await middleware.Invoke(context, _mockTokenService.Object, _mockUserManager.Object, _mockCookieService.Object);

        Mock.Get(_next).Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task Invoke_TokenExpired_ShouldReturn403AndDeleteCookies()
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Cookies = new MockCookieCollection("UserId", "user-id")
            }
        };

        var user = new ApplicationUser("example.url");
        user.SetRefreshToken("valid-refresh-token");
        user.SetRefreshTokenExpires(DateTime.UtcNow.AddHours(1));
        _mockUserManager.Setup(um => um.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);

        var middleware = new RefreshTokenMiddleware(_next);

        await middleware.Invoke(context, _mockTokenService.Object, _mockUserManager.Object, _mockCookieService.Object);

        Assert.Equal(403, context.Response.StatusCode);
        _mockCookieService.Verify(cs => cs.DeleteCookies(), Times.Once);
        Mock.Get(_next).Verify(next => next(context), Times.Never);
    }

    [Fact]
    public async Task Invoke_NoUserId_ShouldCallNext()
    {
        var cookieCollectionMock = new Mock<IRequestCookieCollection>();
        cookieCollectionMock.Setup(c => c["UserId"]).Returns((string)null);

        var context = new DefaultHttpContext
        {
            Request =
            {
                Cookies = cookieCollectionMock.Object
            }
        };

        var middleware = new RefreshTokenMiddleware(_next);

        await middleware.Invoke(context, _mockTokenService.Object, _mockUserManager.Object, _mockCookieService.Object);

        Mock.Get(_next).Verify(next => next(context), Times.Once);
    }
    
    [Fact]
    public async Task Invoke_UserHasNoRefreshTokenExpires_ShouldCallNext()
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Cookies = new MockCookieCollection("UserId", "user-id")
            }
        };

        var user = new ApplicationUser("example.url");
        user.SetRefreshToken("valid-refresh-token");
        _mockUserManager.Setup(um => um.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);

        var middleware = new RefreshTokenMiddleware(_next);

        await middleware.Invoke(context, _mockTokenService.Object, _mockUserManager.Object, _mockCookieService.Object);

        Mock.Get(_next).Verify(next => next(context), Times.Once);
    }
    
    private static string GenerateJwtToken(DateTime expiration)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Guid.NewGuid().ToByteArray());

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "User") }),
            Expires = expiration,
            NotBefore = DateTime.UtcNow,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

public class MockCookieCollection : IRequestCookieCollection
{
    private readonly Dictionary<string, string> _cookies;

    public MockCookieCollection(params string[] keyValuePairs)
    {
        _cookies = new Dictionary<string, string>();

        for (var i = 0; i < keyValuePairs.Length; i += 2)
        {
            _cookies.Add(keyValuePairs[i], keyValuePairs[i + 1]);
        }
    }

    public string this[string key] => _cookies[key];

    public int Count => _cookies.Count;

    public ICollection<string> Keys => _cookies.Keys;

    public bool ContainsKey(string key) => _cookies.ContainsKey(key);

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();

    public bool TryGetValue(string key, out string value) => _cookies.TryGetValue(key, out value);

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _cookies.GetEnumerator();
}
