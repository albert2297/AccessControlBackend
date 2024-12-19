using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AccessControl.Controllers;
using AccessControl.Infrastructure.Persistence.DbContext;
using AccessControl.Infrastructure.Persistence.Entities;
using AccessControl.Application.Dtos.UserEntity;
using AccessControl.Application.Dtos.Common;
using AccessControl.Tests.UnitTests.Mocks;
using AccessControl.Application.Services.BackgroundTaskService;
using Moq;

namespace AccessControl.Tests.UnitTests.Controllers
{
    public class UsersControllerTests
    {
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;

        public UsersControllerTests()
        {
            var backgroundTaskQueueMock = new Mock<IBackgroundTaskQueue>();
            _backgroundTaskQueue = backgroundTaskQueueMock.Object;

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<UserEntity, UserEntityDto>().ReverseMap();
                cfg.CreateMap<UserEntity, UserRegistrationRequestDto>().ReverseMap();
                cfg.CreateMap<UserEntity, UserUpdateRequestDto>().ReverseMap().ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            });
            _mapper = mapperConfig.CreateMapper();

            var configurationMock = new Dictionary<string, string>
            {
                { "JwtSettings:Key", "bP7!wXa9qLmPD3aR$2nVbY6ZtJaH4k9XpQj^T%uA8eC&5ra" },
                { "JwtSettings:Issuer", "TestIssuer" },
                { "JwtSettings:Audience", "TestAudience" },
                { "JwtSettings:ExpirationTime", "1" }
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationMock!)
                .Build();
        }

        [Fact]
        public async Task GetUsers_ReturnsListOfUsers()
        {
            var options = new DbContextOptionsBuilder<AccessControlDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new AccessControlDbContext(options);

            var usersId = new List<Guid>{ Guid.NewGuid(), Guid.NewGuid() };
            var datetime = new List<DateTime> { DateTime.Now, DateTime.Now };

            context.Users.AddRange(
                new UserEntity { Id = usersId[0], CreatedDate = datetime[0], UpdatedDate = datetime[0], FirstName = "John", LastName = "Doe", Email = "johnDoe@example.com" },
                new UserEntity { Id = usersId[1], CreatedDate = datetime[1], UpdatedDate = datetime[1], FirstName = "Jane", LastName = "Smith", Email = "janeSmith@example.com" }
            );
            await context.SaveChangesAsync();

            var userManager = UserManagerMockHelper.MockUserManager(context.Users.AsQueryable());
            var controller = new UsersController(userManager, _mapper, _configuration, _backgroundTaskQueue);

            var result = await controller.GetUsers();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<GenericResponseDto<IEnumerable<UserEntityDto>>>(okResult.Value);

            Assert.NotNull(response.Data);
            var users = response.Data.ToList();
            Assert.Equal(2, users.Count);

            Assert.Equal(usersId[0], users[0].Id);
            Assert.Equal(datetime[0], users[0].CreatedDate);
            Assert.Equal(datetime[0], users[0].UpdatedDate);
            Assert.Equal("John", users[0].FirstName);
            Assert.Equal("Doe", users[0].LastName);
            Assert.Equal("johnDoe@example.com", users[0].Email);

            Assert.Equal(usersId[1], users[1].Id);
            Assert.Equal(datetime[1], users[1].CreatedDate);
            Assert.Equal(datetime[1], users[1].UpdatedDate);
            Assert.Equal("Jane", users[1].FirstName);
            Assert.Equal("Smith", users[1].LastName);
            Assert.Equal("janeSmith@example.com", users[1].Email);
        }

        [Fact]
        public async Task GetUser_ReturnsUser_WhenUserExists()
        {
            var options = new DbContextOptionsBuilder<AccessControlDbContext>()
                 .UseInMemoryDatabase(Guid.NewGuid().ToString())
                 .Options;

            using var context = new AccessControlDbContext(options);

            var userId = Guid.NewGuid();
            var datetime = DateTime.Now;

            context.Users.AddRange(
                new UserEntity { Id = userId, CreatedDate = datetime, UpdatedDate = datetime, FirstName = "John", LastName = "Doe", Email = "johnDoe@example.com" }
            );
            await context.SaveChangesAsync();

            var userManager = UserManagerMockHelper.MockUserManager(context.Users.AsQueryable());
            var controller = new UsersController(userManager, _mapper, _configuration, _backgroundTaskQueue);

            var result = await controller.GetUser(userId.ToString());

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<GenericResponseDto<UserEntityDto>>(okResult.Value);

            Assert.True(response.Success);

            Assert.NotNull(response.Data);
            Assert.Equal(userId, response.Data.Id);
            Assert.Equal(datetime, response.Data.CreatedDate);
            Assert.Equal(datetime, response.Data.UpdatedDate);
            Assert.Equal("John", response.Data.FirstName);
            Assert.Equal("Doe", response.Data.LastName);
            Assert.Equal("johnDoe@example.com", response.Data.Email);
        }

        [Fact]
        public async Task Register_CreatesUser()
        {
            var options = new DbContextOptionsBuilder<AccessControlDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            await using var context = new AccessControlDbContext(options);

            var userManager = UserManagerMockHelper.MockUserManager(context.Users);
            var controller = new UsersController(userManager, _mapper, _configuration, _backgroundTaskQueue);

            var request = new UserRegistrationRequestDto
            {
                Email = "john.doe@example.com",
                Password = "Password123!"
            };

            var result = await controller.Register(request);

            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<GenericResponseDto<object>>(createdAtActionResult.Value);
            Assert.True(response.Success);
        }

        [Fact]
        public async Task Login_ReturnsOk_WhenLoginIsSuccessful()
        {
            var userId = Guid.NewGuid();

            var options = new DbContextOptionsBuilder<AccessControlDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options;

            using var context = new AccessControlDbContext(options);

            context.Users.AddRange(
                new UserEntity { Id = userId, FirstName = "John", LastName = "Doe", Email = "johnDoe@example.com", PasswordHash = "AQAAAAIAAYagAAAAEEnUx3V92hC2EFi6rGZX7O1Ww/cvzWiDnm1UVm2qbM+2TjxfmxWo/xOMxdXPLTYkzg==", SecurityStamp = "5BBKWU6OIV5XCH5LQNOCW4EDQSTING2Q" }
            );
            await context.SaveChangesAsync();

            var updateDto = new UserUpdateRequestDto { FirstName = "Jane", LastName = "Smith", NewPassword = "Password124!" };
            var userManager = UserManagerMockHelper.MockUserManager(context.Users);
            var controller = new UsersController(userManager, _mapper, _configuration, _backgroundTaskQueue);

            var loginDto = new UserLoginRequestDto { Email = "johnDoe@example.com", Password = "Test809*" };

            var result = await controller.Login(loginDto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<GenericResponseDto<TokenResponseDto>>(okResult.Value);

            Assert.NotNull(response.Data);
            Assert.True(response.Success);
            Assert.NotNull(response.Data.Token);
            Assert.True(response.Data.ExpirationTime > DateTime.UtcNow);
        }


        [Fact]
        public async Task UpdateUser_ReturnsNoContent_WhenUpdateIsSuccessful()
        {
            var userId = Guid.NewGuid();

            var options = new DbContextOptionsBuilder<AccessControlDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options;

            using var context = new AccessControlDbContext(options);

            context.Users.AddRange(
                new UserEntity { Id = userId, FirstName = "John", LastName = "Doe", Email = "johnDoe@example.com", PasswordHash= "AQAAAAIAAYagAAAAEEnUx3V92hC2EFi6rGZX7O1Ww/cvzWiDnm1UVm2qbM+2TjxfmxWo/xOMxdXPLTYkzg==", SecurityStamp= "5BBKWU6OIV5XCH5LQNOCW4EDQSTING2Q" }
            );
            await context.SaveChangesAsync();

            var updateDto = new UserUpdateRequestDto { FirstName = "Jane", LastName = "Smith", NewPassword = "Password124!" };
            var userManager = UserManagerMockHelper.MockUserManager(context.Users);
            var controller = new UsersController(userManager, _mapper, _configuration, _backgroundTaskQueue);

            var result = await controller.UpdateUser(userId.ToString(), updateDto);

            Assert.IsType<NoContentResult>(result);
        }


        [Fact]
        public async Task DeleteUser_ReturnsNoContent_WhenUserIsDeleted()
        {
            var options = new DbContextOptionsBuilder<AccessControlDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new AccessControlDbContext(options);

            var user = new UserEntity { Id = Guid.NewGuid(), Email = "john.doe@example.com" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var userManager = UserManagerMockHelper.MockUserManager(context.Users);
            var controller = new UsersController(userManager, _mapper, _configuration, _backgroundTaskQueue);

            var result = await controller.DeleteUser(user.Id.ToString());

            Assert.IsType<NoContentResult>(result);
        }
    }
}
