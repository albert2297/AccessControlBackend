using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using AccessControl.Infrastructure.Persistence.DbContext;
using AccessControl.Application.Dtos.LogEntity;
using AccessControl.Application.Dtos.Common;

namespace AccessControl.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LogsController(AccessControlDbContext context, IMapper mapper) : ControllerBase
    {
        // GET: api/Logs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GenericResponseDto<LogEntityDto>>>> GetLogEntities()
        {
            var logs = await context.LogEntities.Include(log => log.UserEntity).ToListAsync();
            var logDtos = mapper.Map<IEnumerable<LogEntityDto>>(logs);
            var response = new GenericResponseDto<IEnumerable<LogEntityDto>>
            {
                Success = true,
                Message = "Registros recuperados exitosamente.",
                Data = logDtos
            };
            return Ok(response);
        }

        // GET: api/Logs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GenericResponseDto<LogEntityDto>>> GetLogEntity(Guid id)
        {
            var log = await context.LogEntities.Include(log => log.UserEntity).FirstOrDefaultAsync(log => log.Id == id);

            if (log == null)
            {
                return NotFound(new GenericResponseDto<object>
                {
                    Success = false,
                    Message = "El recurso no fue encontrado."
                });
            }
            var logDto = mapper.Map<LogEntityDto>(log);

            var response = new GenericResponseDto<LogEntityDto>
            {
                Success = true,
                Message = "Registro recuperado exitosamente.",
                Data = logDto
            };

            return Ok(response);
        }
    }
}
