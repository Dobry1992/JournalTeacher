using System.ComponentModel.DataAnnotations;

namespace Portal.Models.Election
{
    public class ElectionImage
    {
        [Key]
        public int ElectionImageID {  get; set; }
        public int ElectionArticleID { get; set; }
        public string Title { get; set; }
        public string Path {  get; set; }
        public ElectionArticle ElectionArticle { get; set; }
    }
}
