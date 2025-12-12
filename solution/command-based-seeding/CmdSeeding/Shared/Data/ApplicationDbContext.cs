using Microsoft.EntityFrameworkCore;

namespace CmdSeeding.Shared.Data;

public class ApplicationDbContext : DbContext
{
	public DbSet<User> Users { get; set; }
	
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
		: base(options) { }
	
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<User>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
			entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
			entity.Property(e => e.CreatedAt).IsRequired();
		});
	}
}

public class User
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public DateTime? UpdatedAt { get; set; }
}
