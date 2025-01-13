using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOMIOD.Models
{
    public class Notification
    {


        public int Id { get; set; }

        public string Name { get; set; }
        public int Event { get; set; }
        public string EndPoint { get; set; }

        public bool Enabled { get; set; }

        public DateTime CreationDateTime { get; set; }
        public int Parent { get; set; }
    }
}