using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

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
    }
}
