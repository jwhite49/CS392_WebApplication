using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CS392_WebApplication.Models;

namespace CS392_WebApplication.Data
{
    public class CS392_WebApplicationContext : DbContext
    {
        public CS392_WebApplicationContext (DbContextOptions<CS392_WebApplicationContext> options)
            : base(options)
        {
        }

        public DbSet<CS392_WebApplication.Models.User> User { get; set; } = default!;
    }
}
