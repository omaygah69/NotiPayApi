namespace NotiPayApi.Entities;

public class User
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string HashedPassword { get; set; } = string.Empty;
}
