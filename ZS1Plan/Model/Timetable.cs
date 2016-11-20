using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace ZS1Plan
{
    public class Timetable
    {
        public string name { get; set; }
        public List<Day> days { get; set; }
        public Lesson.LessonType type { get; set; } // 0 - class | 1 - teacher

        public static List<Timetable> GetAllTimeTables(SchoolTimetable st)
        {
            var listOfTimeTables = st.TimetablesOfClasses.ToList();
            listOfTimeTables.AddRange(st.TimetableOfTeachers);
            return listOfTimeTables;
        }

        /// <summary>
        /// Gets Id Of Timetable
        /// </summary>
        /// <returns>absolute id of timetable</returns>
        public static int GetIdOfTimetable(Timetable t, SchoolTimetable st)
        {
            return t == null ?
                -1 :
                t.type == Lesson.LessonType.Teacher ? st.TimetablesOfClasses.Count + st.TimetableOfTeachers.IndexOf(t) : st.TimetablesOfClasses.IndexOf(t);
        }

        /// <summary>
        /// Gets timetable by Id
        /// </summary>
        /// <param name="id">id of timetable</param>
        /// <returns>timetable</returns>
        public static Timetable GetTimetableById(int id, SchoolTimetable st)
        {
            int idOfTimeTable;

            var numOfClassesTimeTables = st.TimetablesOfClasses.Count;

            var type = Lesson.LessonType.Class;

            //we want to translate absolute id to id of Class or id of Teachers
            if (id < numOfClassesTimeTables)
            {
                idOfTimeTable = id;
            }
            else
            {
                idOfTimeTable = id - numOfClassesTimeTables;
                type = Lesson.LessonType.Teacher;
            }

            return type == Lesson.LessonType.Class ? st.TimetablesOfClasses[idOfTimeTable] : st.TimetableOfTeachers[idOfTimeTable];
        }

        /// <summary>
        /// Get latest opened Timetable
        /// </summary>
        /// <returns>latest opened timetable, or null if there is not any other timetable</returns>
        public static Timetable GetLatestOpenedTimeTable(SchoolTimetable st)
        {
            Timetable lastOpenedTimetable = null;

            //First we want to check, if we have any id id class memory
            if (st.IdOfLastOpenedTimeTable != -1)
            {
                //type=0 -> class
                //type=1 -> teacher
                lastOpenedTimetable = GetTimetableById(st.IdOfLastOpenedTimeTable, st);
            }

            //then, we want to check if there is any latest selected plan
            //in application memory
            //application memory is highest priority than a class memory
            if (LocalSettingsServices.ShowTimetableAtStartup.ContainsKey()
                && int.Parse(LocalSettingsServices.ShowTimetableAtStartup.GetKeyValue()) == 1
                && LocalSettingsServices.ShowTimetableAtStartupValue.ContainsKey())
            {
                var nameOfLastSelectedPlan =
                    LocalSettingsServices.ShowTimetableAtStartupValue.GetKeyValue();

                var lot = GetAllTimeTables(st).FirstOrDefault(p => p.name == nameOfLastSelectedPlan);

                if (lot != null)
                {
                    lastOpenedTimetable = lot;
                }
            }
            return lastOpenedTimetable;
        }
    }
}
