using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Model.Entities;

namespace Model.Configurations;

public class ShooterDbContext(DbContextOptions<ShooterDbContext> options) : DbContext(options) {
    public DbSet<HighScore> HighScores { get; set; } = null!;

}