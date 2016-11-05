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
    }
    public class Timetable
    {
        public string name { get; set; }
        public List<Day> days { get; set; }
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
    /*public class Section
    {
        public int idOfSection { get; set; }
        public override string ToString() => MainPage.timetable.timetablesOfClasses[idOfSection].name;
    }
    public class Teacher
    {
        public int idOfTeacher { get; set; }
        public override string ToString() => MainPage.timetable.timetableOfTeachers[idOfTeacher].name;
    }*/
}
