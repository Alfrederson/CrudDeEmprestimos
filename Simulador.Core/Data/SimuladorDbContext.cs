using Microsoft.EntityFrameworkCore;
using Simulador.Core.Models;

namespace Simulador.Core.Data
{
    public class SimuladorDbContext(DbContextOptions<SimuladorDbContext> opt) : DbContext(opt)
    {
        public DbSet<Produto> Produtos { get; set; }
    }
}