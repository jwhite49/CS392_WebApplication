using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CS392_WebApplication.Models;

    public class ProductsDbContext : DbContext
    {
        public ProductsDbContext (DbContextOptions<ProductsDbContext> options)
            : base(options)
        {
        }
        public DbSet<CS392_WebApplication.Models.Products> Products { get; set; } = default!;
        public DbSet<Product_list> Product_list { get; set; }

}
