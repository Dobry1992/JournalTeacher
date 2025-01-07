using Portal.Models;
using System.Collections.Generic;

namespace Portal.ViewModel.Final
{
    public class FinalStudent
    {
        public Student Student { get; set; }
        public List<FinalSubject> FinalSubjects { get; set; }
    }
}
