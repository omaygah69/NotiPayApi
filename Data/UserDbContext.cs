using NotiPayApi.Entities;
using Microsoft.EntityFrameworkCore;
namespace NotiPayApi.Data;

public class UserDb(DbContextOptions<UserDb> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
}
