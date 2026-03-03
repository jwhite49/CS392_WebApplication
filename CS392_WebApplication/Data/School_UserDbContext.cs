using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CS392_WebApplication.Models;

    public class School_UserDbContext : DbContext
{
        public School_UserDbContext (DbContextOptions<School_UserDbContext> options)
            : base(options)
        {
        }
        public DbSet<CS392_WebApplication.Models.User> User { get; set; } = default!;

public DbSet<CS392_WebApplication.Models.School_User> School_User { get; set; } = default!;
}

