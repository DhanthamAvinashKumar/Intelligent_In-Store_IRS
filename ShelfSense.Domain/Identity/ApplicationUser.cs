using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace ShelfSense.Domain.Identity
{
    using Microsoft.AspNetCore.Identity;

    public class ApplicationUser : IdentityUser
    {
        // Inherited properties from IdentityUser:
        //Implementeddomain-specific fields
        public string StoreId { get; set; }
        public string RoleType { get; set; } // Manager, Staff, Warehouse

        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
    }

}
