using AccessControl.Application.Dtos.Common;
using Newtonsoft.Json;

namespace AccessControl.Application.Dtos.UserEntity
{
    public class UserEntityDto : BaseEntityDto
    {
        [JsonProperty(Order = 4)]
        public string FirstName { get; set; } = string.Empty;
        [JsonProperty(Order = 5)]
        public string LastName { get; set; } = string.Empty;
        [JsonProperty(Order = 6)]
        public string Email { get; set; } = string.Empty;
    }
}
