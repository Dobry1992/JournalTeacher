using Portal.Models;
using System.Collections.Generic;

namespace Portal.ViewModel.Statement
{
    public class SubRaiting
    {
        public Subject Subject { get; set; }
        public string Raiting {  get; set; }
        // Средний балл по предмету за период, показывается рядом с итоговой отметкой
        public string AvgRaiting { get; set; }
        public List<Mark> FinalMarks { get; set; }
        public string Color { get; set; }
    }
}
