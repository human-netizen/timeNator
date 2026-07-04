using Microsoft.EntityFrameworkCore;

namespace TimeNator.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options);
