namespace AccessControl.Application.Dtos.Common
{
    public class TokenResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpirationTime { get; set; }
    }
}
