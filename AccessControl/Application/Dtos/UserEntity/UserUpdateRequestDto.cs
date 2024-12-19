namespace AccessControl.Application.Dtos.UserEntity
{
    public class UserUpdateRequestDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? NewPassword { get; set; }
    }
}
