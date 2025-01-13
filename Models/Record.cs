using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOMIOD.Models
{
    public class Record
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string Content { get; set; }

        public DateTime CreationDateTime { get; set; }
        public int Parent { get; set; }
    }
}