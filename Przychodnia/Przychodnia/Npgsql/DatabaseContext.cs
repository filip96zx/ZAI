using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Przychodnia.Models;
using Przychodnia.Npgsql.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Przychodnia.Npgsql
{
    public class DatabaseContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<IdentityUserClaim<int>> IdentityUserClaim { get; set; }

        public DatabaseContext(DbContextOptions<DatabaseContext> options) 
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new RoleConfiguration());
            modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
        }


    }
}