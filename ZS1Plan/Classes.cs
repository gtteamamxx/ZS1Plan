using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace ZS1Plan
{
    public class SchoolTimetable
    {
        public ObservableCollection<Timetable> TimetablesOfClasses { get; set; }
        public ObservableCollection<Timetable> TimetableOfTeachers { get; set; }
        public int IdOfLastOpenedTimeTable { get; set; }

        public SchoolTimetable()
        {
            TimetableOfTeachers = new ObservableCollection<Timetable>();
            TimetablesOfClasses = new ObservableCollection<Timetable>();
        }

        public List<Timetable> GetAllTimeTables()
        {
            var listOfTimeTables = TimetablesOfClasses.ToList();
            listOfTimeTables.AddRange(TimetableOfTeachers);
            return listOfTimeTables;
        }
        /// <summary>
        /// Get latest opened Timetable
        /// </summary>
        /// <returns>latest opened timetable, or null if there is not any other timetable</returns>
        public Timetable GetLatestOpenedTimeTable()
        {
            Timetable lastOpenedTimetable = null;

            //First we want to check, if we have any id id class memory
            if (IdOfLastOpenedTimeTable != -1)
            {
                int idOfTimeTable;

                var numOfClassesTimeTables = TimetablesOfClasses.Count;

                var type = 0;

                //we want to translate absolute id to id of Class or id of Teachers
                if (IdOfLastOpenedTimeTable < numOfClassesTimeTables)
                {
                    idOfTimeTable = IdOfLastOpenedTimeTable;
                }
                else
                {
                    idOfTimeTable = IdOfLastOpenedTimeTable - numOfClassesTimeTables;
                    type = 1;
                }

                //type=0 -> class
                //type=1 -> teacher
                lastOpenedTimetable = type == 0 ? TimetablesOfClasses[idOfTimeTable] : TimetableOfTeachers[idOfTimeTable];
            }

            //then, we want to check if there is any latest selected plan
            //in application memory
            //application memory is highest priority than a class memory
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("ShowTimetableAtStartup")
                && (int.Parse(ApplicationData.Current.LocalSettings.Values["ShowTimetableAtStartup"] as string)) == 1 
                && ApplicationData.Current.LocalSettings.Values.ContainsKey("ShowTimetableAtStartupSelectedPlan"))
            {
                var nameOfLastSelectedPlan =
                    ApplicationData.Current.LocalSettings.Values["ShowTimetableAtStartupSelectedPlan"] as string;

                var lot = GetAllTimeTables().FirstOrDefault(p => p.name == nameOfLastSelectedPlan);

                if (lot != null)
                {
                    lastOpenedTimetable = lot;
                }
            }
            return lastOpenedTimetable;
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
