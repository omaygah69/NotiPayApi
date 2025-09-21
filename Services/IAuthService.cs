using NotiPayApi.Entities;
using NotiPayApi.Models;
using NotiPayApi.Data;
namespace NotiPayApi.Services;

public interface IAuthService
{
    Task<User?> RegisterAsync(UserDto request);
    Task<TokenResponseDto?> LogInAsync(LoginDto request);
    Task<List<User>> GetAllUsersAsync();
}
