using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using censudex.src.models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace censudex.src.data
{
    /// <summary>
    /// Database context for the application, including Identity support.
    /// </summary>
    public class DataContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
    {
        /// <summary>
        /// Initializes a new instance of the DataContext class.
        /// </summary>
        /// <param name="options">The options to be used by a DbContext.</param>
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }
        
        /// <summary>
        /// Configures the model that was discovered by convention from the entity types
        /// exposed in DbSet properties on your derived context.
        /// </summary>
        /// <param name="builder"></param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            List<IdentityRole<Guid>> roles = new List<IdentityRole<Guid>>
            {
                
                new IdentityRole<Guid>
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                },
                new IdentityRole<Guid>
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "User",
                    NormalizedName = "USER"
                }
            };
            builder.Entity<IdentityRole<Guid>>().HasData(roles);
        }
    }
}