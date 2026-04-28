using Microsoft.EntityFrameworkCore;
using TaskDb.Models;

namespace TaskDb.Data;

public class AppDbContext : DbContext {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<TaskItem> Tasks { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<TaskItem>().HasData(
            new TaskItem {
                Id = 1,
                Title = "Изучить ASP.NET Core",
                Description = "Контроллеры, маршруты, middleware",
                Priority = "High",
                IsCompleted = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new TaskItem {
                Id = 2,
                Title = "Подключить SQLite через EF Core",
                Description = "Миграции, DbContext, LINQ-запросы",
                Priority = "High",
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            },
            new TaskItem {
                Id = 3,
                Title = "Написать README",
                Description = "Описать структуру проекта",
                Priority = "Normal",
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}