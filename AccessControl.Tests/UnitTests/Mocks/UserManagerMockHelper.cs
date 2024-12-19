using AccessControl.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AccessControl.Tests.UnitTests.Mocks
{
    public static class UserManagerMockHelper
    {
        public static UserManager<UserEntity> MockUserManager(IEnumerable<UserEntity> users)
        {
            var mockStore = new MockUserStore(users);
            var userManager = new UserManager<UserEntity>(
                mockStore,
                Options.Create(new IdentityOptions()),
                new PasswordHasher<UserEntity>(),
                [],
                [],
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<UserEntity>>>().Object
            );

            var tokenProvider = new Mock<IUserTwoFactorTokenProvider<UserEntity>>();
            userManager.RegisterTokenProvider("Default", tokenProvider.Object);

            tokenProvider.Setup(tp => tp.GenerateAsync(It.IsAny<string>(), It.IsAny<UserManager<UserEntity>>(), It.IsAny<UserEntity>()))
                         .ReturnsAsync("MockToken");

            tokenProvider.Setup(tp => tp.ValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserManager<UserEntity>>(), It.IsAny<UserEntity>()))
                         .ReturnsAsync(true);

            return userManager;
        }
    }

}
