using NotiPayApi.Entities;
using NotiPayApi.Models;
using NotiPayApi.Data;
namespace NotiPayApi.Services;

public interface IAuthService
{
    Task<User?> RegisterAsync(UserDto request);
    Task<string?> LogInAsync(UserDto request);
}
