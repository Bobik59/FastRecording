using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp3.Model
{
    public class Master
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string? FIO { get; set; }
        public string? Description { get; set; }
        public List<Schedule> Schedules { get; set; }
        public List<Bookings> Bookings { get; set; }
    }
}
