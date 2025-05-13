using Portal.Models;

namespace Portal.ViewModel
{
    public class JournalLessons
    {
        public Lesson Lesson { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }
        public bool IsEdit { get; set; }
    }
}
