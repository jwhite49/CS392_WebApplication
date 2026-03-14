using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CS392_WebApplication.Models;

namespace CS392_WebApplication.Data

{
    public class SystemLogDbContext : DbContext
    {
        public SystemLogDbContext (DbContextOptions<SystemLogDbContext> options)
            : base(options)
        {
        }
        public DbSet<CS392_WebApplication.Models.SystemLog> SystemLog { get; set; } = default!;
    }
}
