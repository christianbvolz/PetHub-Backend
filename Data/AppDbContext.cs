using Microsoft.EntityFrameworkCore;
// using pethub.Models;

namespace pethub.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
  // public DbSet<Imagem> Imagens { get; set; }
}