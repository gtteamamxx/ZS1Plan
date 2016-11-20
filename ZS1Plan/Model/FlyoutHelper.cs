using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace ZS1Plan
{
    public static class FlyoutHelper
    {
        public enum ButtonClickedType
        {
            BadButton = -1,
            Place,
            Subject,
            Teacher
        }

        //temp clicked grid
        private static Grid _clickedLessonGrid;
        //temp clicked lesson id; 0 - lesson n0 1 - lesson n1
        private static int _clickedLessonId;

        //When user clicked item and selected lesson
        public delegate void ItemClicked(Lesson clickedLesson, ButtonClickedType buttonType, int lessonId);
        public static event ItemClicked OnItemClicked;

        private static SchoolTimetable _timeTable;

        //sets timetable to private instance 
        public static void SetTimetable(SchoolTimetable st)
        {
            if (_timeTable == null)
            {
                _timeTable = st;
            }
        }

        /// <summary>
        /// Main function which shows flyout with 
        /// added param
        /// </summary>
        /// <param name="lessonGrid">grid where lesson's placed</param>
        /// <param name="buttonParam">buttons</param>
        private static void ShowFlyOutMenu(Grid lessonGrid, params object[] buttonParam)
        {
            var invisibleButton = new Button
            {
                Visibility = Visibility.Collapsed
            };

            lessonGrid.Children.Add(invisibleButton);

            var contentGrid = new Grid();

            for (int i = 0; i < buttonParam.Length; i++)
            {
                contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var buttonFromParam = buttonParam[i] as Button;
                Grid.SetColumn(buttonFromParam, i);

                contentGrid.Children.Add(buttonFromParam);
            }

            var flyout = new Flyout
            {
                Content = contentGrid
            };

            flyout.Opened += (s, e) =>
            {
                //check if lessonGrid has red border brush
                //then set thickness to 2.1, because we have to check
                //somehow 
                var lessonGridBorderBrush = (SolidColorBrush)lessonGrid.BorderBrush;
                lessonGrid.BorderThickness = 
                     lessonGridBorderBrush != null && Colors.Red.Equals(lessonGridBorderBrush.Color) ? new Thickness(2.1) : new Thickness(2.0);

                lessonGrid.BorderBrush = new SolidColorBrush(Colors.Blue);
            };
            flyout.Closed += (s, e) =>
            {
                //if user clicked new lessongrid
                if (_clickedLessonGrid != (Grid)((Button)((Flyout)s).Target).Parent)
                {
                    _clickedLessonId = 0;
                    _clickedLessonGrid = null;
                }

                //if Thickness.Equals(lessonGrid.BorderThickness, new Thickness(2.1));
                if (Math.Abs(lessonGrid.BorderThickness.Bottom - 2.1) < 0.01)
                {
                    lessonGrid.BorderThickness = new Thickness(2.0);
                    lessonGrid.BorderBrush = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    lessonGrid.BorderThickness = new Thickness(1.0);
                    lessonGrid.BorderBrush = new SolidColorBrush(App.Current.RequestedTheme == ApplicationTheme.Light ? Colors.Black : Colors.White);
                }
                lessonGrid.Children.Remove(invisibleButton);
            };

            if (_clickedLessonGrid == null || _clickedLessonGrid != lessonGrid)
            {
                _clickedLessonGrid = lessonGrid;
            }

            flyout.ShowAt(invisibleButton);
        }

        /// <summary>
        /// Shows flyoutmenu for one lesson
        /// </summary>
        /// <param name="lessonGrid">grid where lesson's placed</param>
        public static void ShowFlyOutMenuForLesson(Grid lessonGrid)
        {
            var thicknes5 = new Thickness(5.0);
            var thickness1 = new Thickness(1.0);
            var color = new SolidColorBrush(Colors.Brown);

            var flyoutButtonClass = new Button
            {
                Content = "Pokaż salę",
                Padding = thicknes5,
                Margin = thicknes5,
                BorderBrush = color,
                BorderThickness = thickness1
            };
            flyoutButtonClass.Click += FlyoutButton_Click;

            var flyoutButtonSubject = new Button
            {
                Content = "Pokaż przedmiot",
                Padding = thicknes5,
                Margin = thicknes5,
                BorderBrush = color,
                BorderThickness = thickness1
            };
            flyoutButtonSubject.Click += FlyoutButton_Click;
            Grid.SetColumn(flyoutButtonSubject, 1);

            var flyoutButtonTeacher = new Button
            {
                Content = "Pokaż nauczyciela",
                Padding = thicknes5,
                Margin = thicknes5,
                BorderBrush = color,
                BorderThickness = thickness1
            };
            flyoutButtonTeacher.Click += FlyoutButton_Click;

            ShowFlyOutMenu(lessonGrid, flyoutButtonClass, flyoutButtonSubject, flyoutButtonTeacher);
        }

        public static void ShowFlyOutMenuForTwoLessons(Grid lessonGrid, Lesson lesson)
        {
            var thicknes5 = new Thickness(5.0);
            var thickness1 = new Thickness(1.0);
            var color = new SolidColorBrush(Colors.Brown);

            var flyoutButtonFirstLesson = new Button
            {
                Content = lesson.lesson1Name,
                Padding = thicknes5,
                Margin = thicknes5,
                BorderBrush = color,
                BorderThickness = thickness1
            };
            flyoutButtonFirstLesson.Click += FlyoutButtonFirstLesson_Click; 

            var flyoutButtonSecondLesson = new Button
            {
                Content = lesson.lesson2Name,
                Padding = thicknes5,
                Margin = thicknes5,
                BorderBrush = color,
                BorderThickness = thickness1
            };
            flyoutButtonSecondLesson.Click += FlyoutButtonSecondLesson_Click;

            ShowFlyOutMenu(lessonGrid, flyoutButtonFirstLesson, flyoutButtonSecondLesson);
        }

        /// <summary>
        /// Used when user clicked first lesson in FlyOut (lesson at left)
        /// </summary>
        private static void FlyoutButtonFirstLesson_Click(object sender, RoutedEventArgs e)
        {
            _clickedLessonId = 0;
            ShowFlyOutMenuForLesson(_clickedLessonGrid);
        }
        /// <summary>
        /// Used when user clicked second lesson in FlyOut (lesson at right)
        /// </summary>
        private static void FlyoutButtonSecondLesson_Click(object sender, RoutedEventArgs e)
        {
            _clickedLessonId = 1;
            ShowFlyOutMenuForLesson(_clickedLessonGrid);
        }

        /// <summary>
        /// User clicked "Show sth" in flyout menu
        /// </summary>
        private static void FlyoutButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedLesson = Lesson.GetLessonFromLessonGrid(_clickedLessonGrid, _timeTable);
            var buttonContentString = string.Empty;

            try
            {
                buttonContentString = ((TextBlock)((Button)sender).ContentTemplateRoot).Text ?? "";
            }
            catch
            {
                return;
            }

            int typeOfClickedButton = buttonContentString.Contains("salę") ? 0 :
                buttonContentString.Contains("przedmiot") ? 1 : buttonContentString.Contains("nauczyciela") ? 2 : -1;

            OnItemClicked?.Invoke(selectedLesson, (ButtonClickedType)typeOfClickedButton, _clickedLessonId);
        }
    }
}
