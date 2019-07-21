using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ingos_API.Models
{
    public class User
    {
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string MidName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Town { get; set; }
        public string Technology { get; set; }
        public int RatingCount { get; set; }
        public int RatingValue { get; set; }
    }
}
