using Newtonsoft.Json;

namespace AccessControl.Application.Dtos.LogEntity
{
    public class LogEntityDto
    {
        public Guid Id { get; set; }

        public string EventName { get; set; } = string.Empty;

        public string Details { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        public string Email { get; set; } = string.Empty;

        [JsonProperty("user")]
        public LogUserDto UserEntity { get; set; } = null!;
    }
}
