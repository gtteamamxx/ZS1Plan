using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZS1Plan
{
    public class Day
    {
        public List<Lesson> Lessons { get; set; }
        public int LessonsNum => Lessons.Count();
    }
}
