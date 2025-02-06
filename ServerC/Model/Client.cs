using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerC.Model
{
    public class Client
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string? FIO { get; set; }
    }
}
