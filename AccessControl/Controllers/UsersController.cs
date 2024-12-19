using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using AccessControl.Infrastructure.Persistence.Entities;
using AccessControl.Application.Services.LogService;
using AccessControl.Application.Dtos.Common;
using AccessControl.Application.Dtos.UserEntity;
using AccessControl.Application.Services.EmailService;
using AccessControl.Application.Services.BackgroundTaskService;

namespace AccessControl.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController(UserManager<UserEntity> userManager, IMapper mapper, IConfiguration configuration, IBackgroundTaskQueue taskQueue) : ControllerBase
    {
        // GET: api/users
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<GenericResponseDto<IEnumerable<UserEntityDto>>>> GetUsers()
        {
            var users = await userManager.Users.ToListAsync();
            var userDtos = mapper.Map<IEnumerable<UserEntityDto>>(users);

            var response = new GenericResponseDto<IEnumerable<UserEntityDto>>
            {
                Success = true,
                Message = "Usuarios recuperados exitosamente.",
                Data = userDtos
            };

            return Ok(response);
        }


        // GET: api/users/5
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<GenericResponseDto<UserEntityDto>>> GetUser(string id)
        {
            var user = await userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new GenericResponseDto<object>
                {
                    Success = false,
                    Message = "El recurso no fue encontrado."
                });
            }
            var userDto = mapper.Map<UserEntityDto>(user);
            var response = new GenericResponseDto<UserEntityDto>
            {
                Success = true,
                Message = "Usuario recuperado exitosamente.",
                Data = userDto
            };

            return Ok(response);
        }

        // POST: api/users/register
        [Authorize]
        [HttpPost("register")]
        public async Task<ActionResult<GenericResponseDto<object>>> Register(UserRegistrationRequestDto userRegistrationRequestDto)
        {
            var user = await userManager.FindByEmailAsync(userRegistrationRequestDto.Email);

            if (user != null)
            {
                return BadRequest(new GenericResponseDto<TokenResponseDto>
                {
                    Success = false,
                    Message = "La dirección de correo electrónico existe."
                });
            }

            var datetime = DateTime.Now;
            var UserEntity = mapper.Map<UserEntity>(userRegistrationRequestDto);

            UserEntity.CreatedDate = datetime;
            UserEntity.UpdatedDate = datetime;
            UserEntity.EmailConfirmed = true;
            UserEntity.PhoneNumberConfirmed = false;
            UserEntity.TwoFactorEnabled = true;
            UserEntity.LockoutEnabled = true;
            UserEntity.AccessFailedCount = 0;
            UserEntity.UserName = userRegistrationRequestDto.Email;

            var result = await userManager.CreateAsync(UserEntity, userRegistrationRequestDto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new GenericResponseDto<object>
                {
                    Success = false,
                    Message = "La contraseña no cumple con los requisitos de seguridad. Debe incluir mayúsculas, minúsculas, números y caracteres especiales.",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                });
            }

            return CreatedAtAction(
                nameof(GetUser),
                new { id = UserEntity.Id },
                new GenericResponseDto<object>
                {
                    Success = true,
                    Message = "Usuario recuperado exitosamente."
                }
            );
        }

        // POST: api/users/login
        [HttpPost("login")]
        public async Task<ActionResult<GenericResponseDto<TokenResponseDto>>> Login([FromBody] UserLoginRequestDto loginDTO)
        {
            var user = await userManager.FindByEmailAsync(loginDTO.Email);

            if (user == null)
            {
                return BadRequest(new GenericResponseDto<TokenResponseDto>
                {
                    Success = false,
                    Message = "La dirección de correo electrónico no existe."
                });
            }

            if (!await userManager.CheckPasswordAsync(user, loginDTO.Password))
            {
                return Unauthorized(new GenericResponseDto<object>
                {
                    Success = false,
                    Message = "Credenciales inválidas."
                });
            }

            var tokenHandler = new JwtSecurityTokenHandler();

            var jwtSettingsKey = configuration.GetValue<string>("JwtSettings:Key");

            if (string.IsNullOrEmpty(jwtSettingsKey))
            {
                 return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "Falta la clave de firma JWT en la configuración."
                 });
            }

            var key = Encoding.UTF8.GetBytes(jwtSettingsKey);

            var claims = new List<Claim>
            {
                new (ClaimTypes.Name, user.FirstName ?? ""),
                new (ClaimTypes.Surname, user.LastName ?? ""),
                new (ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var expirationTime = DateTime.UtcNow.AddHours(
                configuration.GetValue<int>("JwtSettings:ExpirationTime")
            );

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expirationTime,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = configuration.GetValue<string>("JwtSettings:Issuer"),
                Audience = configuration.GetValue<string>("JwtSettings:Audience")
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            var logServiceRequest = new LogServiceRequest
            {
                EventName= "Inicio de sesión del usuario exitoso",
                Details = $"{user.UserName} ha iniciado sesión exitosamente.",
                UserId = user.Id,
                Email = user.Email!
            };

            taskQueue.QueueBackgroundWorkItem(async serviceProvider =>
            {
                var scopedLogService = serviceProvider.GetRequiredService<ILogService>();
                await scopedLogService.LogToDatabaseAsync(logServiceRequest);
            });

            var emailServiceRequest = new EmailServiceRequest
            {
                Email = loginDTO.Email
            };

            taskQueue.QueueBackgroundWorkItem(async serviceProvider =>
            {
                var scopedEmailService = serviceProvider.GetRequiredService<IEmailService>();
                await scopedEmailService.SendEmailAsync(emailServiceRequest);
            });

            return Ok(new GenericResponseDto<TokenResponseDto>
            {
                Success = true,
                Message = "Usuario iniciado sesión exitosamente.",
                Data = new TokenResponseDto
                {
                    Token = tokenHandler.WriteToken(token),
                    ExpirationTime = expirationTime
                }
            });
        }

        // PUT: api/users/5
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UserUpdateRequestDto updateDto)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new GenericResponseDto<object>
                {
                    Success = false,
                    Message = "Usuario no encontrado."
                });
            }

            mapper.Map(updateDto, user);

            if (!string.IsNullOrEmpty(updateDto.NewPassword))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await userManager.ResetPasswordAsync(user, token, updateDto.NewPassword);
                if (!passwordResult.Succeeded)
                {
                    return BadRequest(new GenericResponseDto<object>
                    {
                        Success = false,
                        Message = "Restablecimiento de contraseña fallido.",
                        Errors = passwordResult.Errors.Select(e => e.Description).ToList()
                    });
                }
            }

            user.UpdatedDate = DateTime.Now;
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new GenericResponseDto<object>
                {
                    Success = false,
                    Message = "Actualización de usuario fallida.",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                });
            }

            return NoContent();
        }

        // DELETE: api/users/5
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new GenericResponseDto<object>
                {
                    Success = false,
                    Message = "Usuario no encontrado."
                });
            }

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new GenericResponseDto<object>
                {
                    Success = false,
                    Message = "Eliminación de usuario fallida.",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                });
            }

            return NoContent();
        }
    }
}
