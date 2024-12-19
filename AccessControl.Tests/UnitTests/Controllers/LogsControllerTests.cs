using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using AccessControl.Controllers;
using AccessControl.Infrastructure.Persistence.DbContext;
using AccessControl.Application.Dtos.LogEntity;
using AccessControl.Application.Dtos.Common;
using AccessControl.Infrastructure.Persistence.Entities;

namespace AccessControl.Tests.UnitTests.Controllers
{
    public class LogsControllerTests
    {
        private readonly IMapper _mockMapper;

        public LogsControllerTests()
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<LogEntity, LogEntityDto>()
                    .ForMember(dest => dest.UserEntity, opt => opt.MapFrom(src => src.UserEntity == null ? null : new LogUserDto
                    {
                        Id = src.UserEntity.Id,
                        FirstName = src.UserEntity.FirstName,
                        LastName = src.UserEntity.LastName
                    }));
            });

            _mockMapper = mapperConfig.CreateMapper();
        }

        [Fact]
        public async Task GetLogEntities_ReturnsOkWithLogsAndUserDetails()
        {
            var options = new DbContextOptionsBuilder<AccessControlDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new AccessControlDbContext(options);

            var usersId = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var logsId = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var datetime = new List<DateTime> { DateTime.Now, DateTime.Now };

            var user1 = new UserEntity { Id = usersId[0], FirstName = "John", LastName = "Doe", Email = "johnDoe@example.com" };
            var user2 = new UserEntity { Id = usersId[1], FirstName = "Jane", LastName = "Smith", Email = "janeSmith@example.com" };

            var logs = new List<LogEntity>
            {
                new() { Id = logsId[0], EventName = "Inicio de sesión", Details = "Inicio de sesión del usuario exitoso.", Timestamp = datetime[0], Email = "johnDoe@example.com", UserEntity = user1, UserId = user1.Id
                },
                new() { Id = logsId[1], EventName = "Cierre de sesión", Details = "Cierre de sesión del usuario exitoso.", Timestamp = datetime[1], Email = "janeSmith@example.com", UserEntity = user2, UserId = user2.Id
                }
            };

            context.Users.AddRange(user1, user2);
            context.LogEntities.AddRange(logs);
            await context.SaveChangesAsync();

            var controller = new LogsController(context, _mockMapper);

            var result = await controller.GetLogEntities();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<GenericResponseDto<IEnumerable<LogEntityDto>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Registros recuperados exitosamente.", response.Message);

            Assert.NotNull(response.Data);
            var data = response.Data.ToList();
            Assert.Equal(2, data.Count);

            Assert.Equal(logsId[0], data[0].Id);
            Assert.Equal("Inicio de sesión", data[0].EventName);
            Assert.Equal("Inicio de sesión del usuario exitoso.", data[0].Details);
            Assert.Equal(datetime[0], data[0].Timestamp);
            Assert.Equal(usersId[0], data[0].UserEntity.Id);
            Assert.Equal("John", data[0].UserEntity.FirstName);
            Assert.Equal("Doe", data[0].UserEntity.LastName);
            Assert.Equal("johnDoe@example.com", data[0].Email);

            Assert.Equal(logsId[1], data[1].Id);
            Assert.Equal("Cierre de sesión", data[1].EventName);
            Assert.Equal("Cierre de sesión del usuario exitoso.", data[1].Details);
            Assert.Equal(datetime[1], data[1].Timestamp);
            Assert.Equal(usersId[1], data[1].UserEntity.Id);
            Assert.Equal("Jane", data[1].UserEntity.FirstName);
            Assert.Equal("Smith", data[1].UserEntity.LastName);
            Assert.Equal("janeSmith@example.com", data[1].Email);
        }

        [Fact]
        public async Task GetLogEntity_ReturnsOk_WhenLogExists()
        {
            var options = new DbContextOptionsBuilder<AccessControlDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new AccessControlDbContext(options);

            var userId = Guid.NewGuid();
            var logId = Guid.NewGuid();
            var datetime = DateTime.Now;

            var user = new UserEntity { Id = userId, FirstName = "John", LastName = "Doe", Email = "johnDoe@example.com" };

            var log =  new LogEntity { Id = logId, EventName = "Inicio de sesión", Details = "Inicio de sesión del usuario exitoso.", Timestamp = datetime, Email = "johnDoe@example.com", UserEntity = user, UserId = userId };

            context.Users.Add(user);
            context.LogEntities.Add(log);
            await context.SaveChangesAsync();

            var controller = new LogsController(context, _mockMapper);
            var result = await controller.GetLogEntity(log.Id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<GenericResponseDto<LogEntityDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Registro recuperado exitosamente.", response.Message);
            Assert.NotNull(response.Data);
            var data = response.Data;
            Assert.Equal(logId, data.Id);
            Assert.Equal("Inicio de sesión", data.EventName);
            Assert.Equal("Inicio de sesión del usuario exitoso.", data.Details);
            Assert.Equal(datetime, data.Timestamp);
            Assert.Equal(userId, data.UserEntity.Id);
            Assert.Equal("John", data.UserEntity.FirstName);
            Assert.Equal("Doe", data.UserEntity.LastName);
            Assert.Equal("johnDoe@example.com", data.Email);
        }
    }
}
