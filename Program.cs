using System.Text;
using Scalar.AspNetCore;
using NotiPayApi.Data;
using NotiPayApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient("xendit", c =>
{
    c.BaseAddress = new Uri("https://api.xendit.co/");
    // Use the LATEST API version globally
    c.DefaultRequestHeaders.Add("x-api-version", "2023-10-01");
    // Add User-Agent for better API compatibility
    c.DefaultRequestHeaders.Add("User-Agent", "NotiPayApi/1.0");
});
builder.Services.AddScoped<IPaymentNoticeService, PaymentNoticeService>();
builder.Services.AddScoped<IXenditService, XenditService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddDbContext<UserDb>(options =>
    options.UseSqlite("Data Source=user.db"));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["AppSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["AppSettings:Audience"],
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["AppSettings:Token"]!)),
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddOpenApi("v1");
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/hello", () => "Hello World from .NET API!");

app.UseHttpsRedirection();
app.UseAuthentication();  
app.UseAuthorization();
app.MapControllers();

app.Run();
