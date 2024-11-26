using System.ComponentModel.DataAnnotations;

namespace Portal.Models.Menu
{
    public class Menu
    {
        [Key]
        public int MenuID { get; set; }
        public string Title { get; set; }
        public string Path { get; set; }
    }
}
