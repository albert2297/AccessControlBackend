using Newtonsoft.Json;

namespace AccessControl.Application.Dtos.Common
{
    public class BaseEntityDto
    {
        [JsonProperty(Order = 1)]
        public Guid Id { get; set; }
        [JsonProperty(Order = 2)]
        public DateTime CreatedDate { get; set; }
        [JsonProperty(Order = 3)]
        public DateTime UpdatedDate { get; set; }
    }
}
