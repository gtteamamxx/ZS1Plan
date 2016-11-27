using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace ZS1Plan
{
    public class Lesson
    {
        public enum LessonType
        {
            Class = 0,
            Teacher = 1
        }

        public struct dayCoordiantes
        {
            public int dayId, lessonId, timetableId;
        }

        public bool IsLessonTeacherLesson() => !string.IsNullOrEmpty(lesson2Name) && string.IsNullOrEmpty(lesson2Tag);

        public dayCoordiantes lessonDayPosition { get; set; } // 0 -> monday, 1 - tuesday

        public string lesson1Name { get; set; }
        public string lesson1Place { get; set; }
        public string lesson1Tag { get; set; }
        public string lesson1TagHref { get; set; }
        public string lesson2Name { get; set; }
        public string lesson2Place { get; set; }
        public string lesson2Tag { get; set; }
        public string lesson2TagHref { get; set; }

        /// <summary>
        /// Gets ID of lesson from lesson Grid
        /// </summary>
        /// <param name="lessonGrid">grid where lesson's placed</param>
        /// <returns>Lesson index</returns>
        public static Lesson GetLessonFromLessonGrid(Grid lessonGrid, SchoolTimetable st)
        {
            var splittedText = ((TextBlock)lessonGrid.Children.First(p => p is TextBlock && ((TextBlock)p).Text.Contains("[]"))).Text.
                Split(' ');

            int jinloop = int.Parse(splittedText[1]);
            int iinloop = int.Parse(splittedText[2]);

            var t = Timetable.GetTimetableById(int.Parse(splittedText[3]), st);

            return t.days[jinloop - 2].Lessons[iinloop - 1];
        }
    }

}
