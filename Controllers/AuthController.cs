using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using NotiPayApi.Models;
using NotiPayApi.Entities;
using NotiPayApi.Services;
using System.Security.Claims;
namespace NotiPayApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authservice) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<User>> Register(UserDto request)
    {
	var user = await authservice.RegisterAsync(request);
	if(user is null)
	    return BadRequest("[ERROR] Username Already Exists");
	return Ok(user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> LogIn(LoginDto request)
    {
	var result = await authservice.LogInAsync(request);
	if(result is null)
	    return BadRequest("[ERROR] Invalid Username or Password");
	return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin-check")]
    public IActionResult CheckAdmin()
    {
	return Ok("User is Admin");
    }

    [Authorize]
    [HttpGet]
    public IActionResult CheckAuthenticated()
    {
	return Ok("User is authenticated");
    }

   [Authorize(Roles = "Admin")]
   [HttpGet("members")]
   public async Task<ActionResult<List<UserResponseDto>>> GetMembers()
   {
       // Access the DbContext via your AuthService or inject it directly here
       var users = await authservice.GetAllUsersAsync();

       // Map to DTO
       var response = users.Select(u => new UserResponseDto
       {
           Id = u.Id,
           UserName = u.UserName,
           Role = u.Role
       }).ToList();

       return Ok(response);
   }


//     private string CreateToken(User user)
//     {
// 	List<Claim> claims = new List<Claim>
// 	{
// 	    new Claim(ClaimTypes.Name, user.UserName),
// 	};
// 	SymmetricSecurityKey key = new(
// 	    Encoding.UTF8.GetBytes(configuration.GetValue<string>("AppSettings:Token")!));
// 	var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
// 	var token_descriptor = new JwtSecurityToken(
// 	    issuer: configuration.GetValue<string>("AppSettings:Issuer"),
// 	    audience: configuration.GetValue<string>("AppSettings:Audience"),
// 	    claims: claims,
// 	    expires: DateTime.UtcNow.AddDays(3),
// 	    signingCredentials: creds
// 	);

// 	return new JwtSecurityTokenHandler().WriteToken(token_descriptor);
//     }
}
