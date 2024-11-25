using System;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models.Birthday
{
    public class Birthday
    {
        [Key]
        public int BirthdayID { get; set; }
        public string Title { get; set; }
        public string Path { get; set; }
        public DateTime Date { get; set; }
        public DateTime DateBirth { get; set; }
    }
}
