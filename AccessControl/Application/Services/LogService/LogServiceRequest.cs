using AccessControl.Infrastructure.Persistence.Entities;

namespace AccessControl.Application.Services.LogService
{
    public class LogServiceRequest
    {
        public string EventName { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
    }
}
