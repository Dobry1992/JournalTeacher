using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Portal.Models;

namespace Portal.ViewModel
{
    public class SubgroupCreateViewModel
    {
        public int GroupID { get; set; }
        public string GroupName { get; set; }
        public int SubjectID { get; set; }
        public string SubjectName { get; set; }

        [Required(ErrorMessage = "Необходимо указать название подгруппы")]
        [StringLength(255, ErrorMessage = "Название не может превышать 255 символов")]
        [Display(Name = "Название подгруппы")]
        public string SubgroupName { get; set; }

        public List<Student> AvailableStudents { get; set; } = new List<Student>();
        public List<int> SelectedStudentIDs { get; set; } = new List<int>();
    }
}