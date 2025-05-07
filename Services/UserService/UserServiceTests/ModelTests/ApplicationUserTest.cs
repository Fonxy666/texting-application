// using Microsoft.AspNetCore.Identity;
// using UserService.Models;
//
// namespace Tests.ModelTests;
//
// [TestFixture]
// public class ApplicationUserTests
// {
//     [Test]
//     public void ApplicationUser_Creation_Succeeds()
//     {
//         const string imageUrl = "https://example.com/image.jpg";
//         
//         var user = new ApplicationUser(imageUrl);
//         
//         Assert.AreEqual(imageUrl, user.ImageUrl);
//     }
//
//     [Test]
//     public void ApplicationUser_Inherits_IdentityUser()
//     {
//         const string imageUrl = "https://example.com/image.jpg";
//         
//         var user = new ApplicationUser(imageUrl);
//         
//         Assert.IsInstanceOf<IdentityUser<Guid>>(user);
//     }
//
//     [Test]
//     public void ApplicationUser_ImageUrl_Set_Private()
//     {
//         const string imageUrl = "https://example.com/image.jpg";
//         var user = new ApplicationUser(imageUrl);
//         
//         var property = typeof(ApplicationUser).GetProperty("ImageUrl");
//         
//         Assert.NotNull(property);
//         Assert.IsTrue(property.GetSetMethod(true)!.IsPrivate);
//     }
// }
