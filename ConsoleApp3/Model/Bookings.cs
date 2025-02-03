using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp3.Model
{
    public class Bookings
    {
        public int Id { get; set; }
        public Client Client { get; set; }
        public Master Master { get; set; }
        public DateTime BookingTime { get; set; }
        public string Service {  get; set; }
        public int Price { get; set; }
    }
}
