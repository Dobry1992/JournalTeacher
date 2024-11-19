using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models.Election
{
    public class ElectionArticle
    {
        [Key]
        public int ElectionArticleID { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public DateTime Date { get; set; }
        public ICollection<ElectionImage> Images { get; set; }
    }
}
