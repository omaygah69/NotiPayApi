namespace NotiPayApi.Models;

public class UserResponseDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public string PhoneNumber { get; set; } = string.Empty;
}

