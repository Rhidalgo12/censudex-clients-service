using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bogus.DataSets;
using Microsoft.AspNetCore.Identity;

namespace censudex.src.models
{
    public class AppUser : IdentityUser<Guid>
    {
        public override Guid Id { get; set; } = Guid.NewGuid();

        public string FullName { get; set; } = string.Empty;

        public DateOnly DateOfBirth { get; set; }

        public string Address { get; set; } = string.Empty;
        
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}