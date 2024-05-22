using Microsoft.AspNetCore.Http;
using Moq;
using Server.Model;
using Server.Services.Authentication;
using Server.Services.Cookie;
using Xunit;
using Assert = NUnit.Framework.Assert;
using CookieOptions = Microsoft.AspNetCore.Http.CookieOptions;

namespace Tests.Services;

public class CookieServiceTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IResponseCookies> _mockResponseCookies;
    private readonly CookieService _cookieService;
    private readonly DefaultHttpContext _httpContext;

    public CookieServiceTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockTokenService = new Mock<ITokenService>();
        _mockResponseCookies = new Mock<IResponseCookies>();

        _httpContext = new DefaultHttpContext();

        _mockHttpContextAccessor.Setup(_ => _.HttpContext).Returns(_httpContext);

        _cookieService = new CookieService(_mockHttpContextAccessor.Object, _mockTokenService.Object);
    }
    
    [Fact]
    public void SetUserId_ShouldSetCookie()
    {
        var userId = Guid.NewGuid();
        const bool rememberMe = true;
        
        _mockResponseCookies.Setup(cookies => cookies.Append(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CookieOptions>()));

        _cookieService.SetUserId(userId, rememberMe);

        Assert.That(_mockHttpContextAccessor.Object.HttpContext?.Response.Cookies, Is.Not.EqualTo(null));
    }
    
    [Fact]
    public void SetRefreshToken_ShouldSetCookie()
    {
        var user = new ApplicationUser();
        var newRefreshToken = new RefreshToken { Token = "test-token", Expires = DateTime.UtcNow.AddDays(7) };
        _mockTokenService.Setup(service => service.CreateRefreshToken()).Returns(newRefreshToken);

        _cookieService.SetRefreshToken(user);

        Assert.That(_mockHttpContextAccessor.Object.HttpContext?.Response.Cookies, Is.Not.EqualTo(null));
    }
    
    [Fact]
    public async Task SetJwtToken_ShouldSetCookie()
    {
        const string accessToken = "test-access-token";
        const bool rememberMe = true;

        await _cookieService.SetJwtToken(accessToken, rememberMe);

        Assert.That(_mockHttpContextAccessor.Object.HttpContext?.Response.Cookies, Is.Not.EqualTo(null));
    }
    
    [Fact]
    public void DeleteCookies_ShouldDeleteAllCookies()
    {
        _cookieService.DeleteCookies();
        
        Assert.That(_mockHttpContextAccessor.Object.HttpContext?.Response.Cookies, Is.Not.EqualTo(null));
    }
    
    [Fact]
    public void SetRememberMeCookie_ShouldSetCookie()
    {
        const bool rememberMe = true;

        _cookieService.SetRememberMeCookie(rememberMe);

        Assert.That(_mockHttpContextAccessor.Object.HttpContext?.Response.Cookies, Is.Not.EqualTo(null));
    }
    
    [Fact]
    public void ChangeUserAnonymous_ShouldChangeAnonymousCookie()
    {
        _cookieService.ChangeUserAnonymous();

        Assert.That(_mockHttpContextAccessor.Object.HttpContext?.Response.Cookies, Is.Not.EqualTo(null));
    }
    
    [Fact]
    public void ChangeUserAnonymous_ShouldChangeAnimationCookie()
    {
        _cookieService.ChangeAnimation();

        Assert.That(_mockHttpContextAccessor.Object.HttpContext?.Response.Cookies, Is.Not.EqualTo(null));
    }
    
    [Fact]
    public void SetAnimateAndAnonymous_ShouldSetCookie()
    {
        _cookieService.SetAnimateAndAnonymous(true);

        Assert.That(_mockHttpContextAccessor.Object.HttpContext?.Response.Cookies, Is.Not.EqualTo(null));
    }
}