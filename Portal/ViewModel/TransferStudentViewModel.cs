using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Portal.ViewModel
{
    public class TransferStudentViewModel
    {
        public int StudentID { get; set; }

        public string LastName { get; set; }

        public string Name { get; set; }

        public string Surname { get; set; }

        public int OldGroupID { get; set; }

        public string OldGroupName { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Выберите группу, в которую выполняется перевод")]
        [Display(Name = "Группа перевода")]
        public int NewGroupID { get; set; }

        [Display(Name = "Комментарий")]
        [StringLength(400, ErrorMessage = "Комментарий не может превышать 400 символов")]
        public string Comment { get; set; }

        public IEnumerable<SelectListItem> Groups { get; set; } = new List<SelectListItem>();
    }
}
