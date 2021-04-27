using Microsoft.AspNetCore.Identity;
using Przychodnia.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Przychodnia.Models
{
    public class User : IdentityUser<int>
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public Gender Gender { get; set; }
        public string PESEL { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; }
        public virtual ICollection<IdentityUserClaim<int>> Claims { get; set; }
        public bool IsAdmin()
        {
            return UserRoles != null
                ? UserRoles.Any(prop => prop.Role.Name.ToLower() == "admin")
                : false;
        }


    }
}
