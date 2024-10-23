using System.ComponentModel.DataAnnotations;

namespace Portal.Models
{
    public class Image
    {
        [Key]
        public int ImageID { get; set; }
        public int ArticleID { get; set; }
        public string Title { get; set; }
        public string Path { get; set; }
        public Article Article { get; set; }
    }
}
