using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class Article
    {
        [Key]
        public int ArticleID { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public DateTime DateOfNews { get; set; }
        public ICollection<Image> Images { get; set; }
    }
}
