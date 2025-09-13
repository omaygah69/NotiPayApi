using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using NotiPayApi.Models;
using NotiPayApi.Entities;
using NotiPayApi.Services;
using NotiPayApi.Data;
using System.Security.Claims;
using System.Security.Cryptography;
namespace NotiPayApi.Services;

public class AuthService(UserDb context, IConfiguration configuration) : IAuthService
{
    public async Task<User?> RegisterAsync(UserDto request)
    {
	if(await context.Users.AnyAsync(u => u.UserName == request.UserName))
	    return null;

	User user = new();
	var hashed_password = new PasswordHasher<User>().
	    HashPassword(user, request.Password);
	user.UserName = request.UserName;
	user.HashedPassword = hashed_password;
	user.Role = "User";

	context.Users.Add(user);
	await context.SaveChangesAsync();

	return user;
    }
    
    public async Task<TokenResponseDto?> LogInAsync(UserDto request)
    {
	var user = await context.Users.FirstOrDefaultAsync(u =>
							   u.UserName == request.UserName);
	if(user is null)
	    return null;
	var verify_hash = new PasswordHasher<User>()
	    .VerifyHashedPassword(user, user.HashedPassword, request.Password);
	if(verify_hash == PasswordVerificationResult.Failed)
	    return null;

	var response = new TokenResponseDto {
	    AccessToken = CreateToken(user),
	    RefreshToken = await GenerateAndSaveRefreshTokenAsync(user),
	};
	return response;
	
    }
    
    private string CreateToken(User user)
    {
	List<Claim> claims = new List<Claim>
	{
	    new Claim(ClaimTypes.Name, user.UserName),
	    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
	    new Claim(ClaimTypes.Role, user.Role),
	};
	
	SymmetricSecurityKey key = new(
	    Encoding.UTF8.GetBytes(configuration.GetValue<string>("AppSettings:Token")!));
	var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
	var token_descriptor = new JwtSecurityToken(
	    issuer: configuration.GetValue<string>("AppSettings:Issuer"),
	    audience: configuration.GetValue<string>("AppSettings:Audience"),
	    claims: claims,
	    expires: DateTime.UtcNow.AddDays(3),
	    signingCredentials: creds
	);
	return new JwtSecurityTokenHandler().WriteToken(token_descriptor);
    }

    private string GenerateRefreshToken()
    {
	byte[] randNum = new byte[32];
	using var rng = RandomNumberGenerator.Create();
	rng.GetBytes(randNum);
	return Convert.ToBase64String(randNum);
    }

    private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
    {
	string refreshToken = GenerateRefreshToken();
	user.RefreshToken = refreshToken;
	user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
	await context.SaveChangesAsync();
	return refreshToken;
    }
}
