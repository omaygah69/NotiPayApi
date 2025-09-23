using NotiPayApi.Entities;
using Microsoft.EntityFrameworkCore;
using NotiPayApi.Models;  
namespace NotiPayApi.Data;

public class UserDb(DbContextOptions<UserDb> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<PaymentNotice> PaymentNotices { get; set; } = null!;
    public DbSet<XenditPayment> XenditPayments { get; set; } = null!;
    public DbSet<SchoolYear> SchoolYears { get; set; }
    public DbSet<Appointment> Appointments { get; set; }  // If implementing appointments
}
