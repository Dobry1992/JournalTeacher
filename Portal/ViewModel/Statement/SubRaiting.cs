using Portal.Models;
using System.Collections.Generic;

namespace Portal.ViewModel.Statement
{
    public class SubRaiting
    {
        public Subject Subject { get; set; }
        public string Raiting {  get; set; }
        public List<Mark> FinalMarks { get; set; }
        public string Color { get; set; }
    }
}
