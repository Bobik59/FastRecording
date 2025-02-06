﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerC.Model
{
    public class Schedule
    {
        public int Id { get; set; }
        public Client Client { get; set; }
        public Master Master { get; set; }
        public bool IsAccepted { get; set; }
        public string Service { get; set; }
        public DateTime BookingTime { get; set; }
    }
}
