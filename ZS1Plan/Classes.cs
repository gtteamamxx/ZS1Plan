using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZS1Plan
{
    public class SchoolTimetable
    {
        public ObservableCollection<Timetable> timetablesOfClasses { get; set; }
        public ObservableCollection<Timetable> timetableOfTeachers { get; set; }
        public int idOfLastOpenedTimeTable { get; set; }

        public Timetable GetLatestOpenedTimeTable()
        {
            if (idOfLastOpenedTimeTable == -1)
            {
                return null;
            }

            int idOfTimeTable;

            var numOfClassesTimeTables = timetablesOfClasses.Count;

            var type = 0;
            if (idOfLastOpenedTimeTable < numOfClassesTimeTables)
            {
                idOfTimeTable = idOfLastOpenedTimeTable;
            }
            else
            {
                idOfTimeTable = idOfLastOpenedTimeTable - numOfClassesTimeTables;
                type = 1;
            }

            return type == 0 ? timetablesOfClasses[idOfTimeTable] : timetableOfTeachers[idOfTimeTable];
        }
    }
    public class Timetable
    {
        public string name { get; set; }
        public List<Day> days { get; set; }
        public int type { get; set; } // 0 - class | 1 - teacher
    }
    public class Day
    {
        public List<Lesson> Lessons { get; set; }
        public int lessonsNum => Lessons.Count();
    }
    public class Lesson
    {
        public string lesson1Name { get; set; }
        public string lesson1Place { get; set; }
        public string lesson1Tag { get; set; }
        public string lesson1TagHref { get; set; }
        public string lesson2Name { get; set; }
        public string lesson2Place { get; set; }
        public string lesson2Tag { get; set; }
        public string lesson2TagHref { get; set; }
    }
}
