using Microsoft.EntityFrameworkCore;
using CS392_WebApplication.Models;

namespace CS392_WebApplication.Data
{
    public class Product_listDbContext : DbContext
    {
        public Product_listDbContext(DbContextOptions<Product_listDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product_list> Product_list { get; set; }

    }
}
