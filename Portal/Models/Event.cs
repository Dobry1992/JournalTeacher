using System;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class Event
    {
        [Key]
        public int EventID { get; set; }
        public string Log { get; set; }
        public string Teacher { get; set; }
        public DateTime Date { get; set; }
    }
}
