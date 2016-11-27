using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace ZS1Plan
{
    public sealed partial class MainPage
    {
        //Main Timetable intance
        public static SchoolTimetable TimeTable = new SchoolTimetable();

        //timetables used in listview
        private static ObservableCollection<Timetable> TimeTableOfSections => TimeTable.TimetablesOfClasses;
        private static ObservableCollection<Timetable> TimeTableOfTeachers => TimeTable.TimetableOfTeachers;

        private readonly string[] _dayNames = { "Nr", "Godz", "Poniedziałek", "Wtorek", "Środa", "Czwartek",
                                  "Piątek" };

        private readonly string[] _lessonTimes = { "7:10 - 7:55", "8:00 - 8:45", "8:50 - 9:35", "9:45 - 10:30",
                                   "10:45 - 11:30", "11:35 - 12:20", "12:30 - 13:15", "13:20 - 14:05",
                                    "14:10 - 14:55", "15:00 - 15:45", "15:50 - 16:35" };
        private bool _isLoaded;

        private static MainPage _gui;

        public static void SetTitleText(string text) => _gui.TitleText.Text = text;

        public static Visibility InfoCenterStackPanelVisibility
        {
            get
            {
                return _gui.InfoCenterStackPanel.Visibility;
            }
            set
            {
                _gui.InfoCenterStackPanel.Visibility = value;
            }
        }

        public MainPage()
        {
            InitializeComponent();

            _gui = this;

            //if windows phone
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                StatusBar.GetForCurrentView().ForegroundColor = Colors.White;
                StatusBar.GetForCurrentView().BackgroundOpacity = 1;
                StatusBar.GetForCurrentView().BackgroundColor = Colors.Black;
            }

            Loaded += MainPage_Loaded;


            //If windows phone then register hardware back button and proper event
            if (ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
            {
                BackButton.Visibility = Visibility.Collapsed;

                Windows.Phone.UI.Input.HardwareButtons.BackPressed += async (s, e) =>
                {
                    e.Handled = await GoBack();
                };

                return;
            }

            BackButton.Click += async (s, e) =>
            {
                if (!(await GoBack()))
                    Application.Current.Exit();
            };
        }

        private async Task<bool> GoBack()
        {
            //if settings page is opened
            var tuple = PagesManager.GetPage();

            var beforeTuple = PagesManager.GetPageWithoutDelete();

            //there's no item in queue, so exit app
            if (tuple == null || beforeTuple == null
                || (beforeTuple != null && beforeTuple.Item1 == PagesManager.ePagesType.Timetable && beforeTuple.Item2 == tuple.Item2))
                return false;

            var backPageType = tuple.Item1;

            if (backPageType == PagesManager.ePagesType.SettingsPage) // if last object was settings page, then
            {
                //get again last one, because we are currently in settings page, and we want to go back, so we have to
                //have element before it
                tuple = PagesManager.GetPage();
                beforeTuple = PagesManager.GetPageWithoutDelete();

                if (tuple == null)
                    return false;

                backPageType = tuple.Item1;

                SplitViewContentScrollViewer.Visibility = Visibility.Visible;
                SplitViewContentFrame.Visibility = Visibility.Collapsed;
            }
            else if(backPageType == PagesManager.ePagesType.TimeTable_Place)
            {
                tuple = PagesManager.GetPage();

                if (tuple == null)
                    return false;

                backPageType = tuple.Item1;
            }
            else
            {
                tuple = PagesManager.GetPageWithoutDelete();
                backPageType = tuple.Item1;
                if (backPageType == PagesManager.ePagesType.SettingsPage)
                {
                    SplitViewContentScrollViewer.Visibility = Visibility.Collapsed;
                    SplitViewContentFrame.Visibility = Visibility.Visible;

                    SplitViewContentFrame.Navigate(typeof(SettingsPage));
                    return true;
                }
            }

            if(backPageType == PagesManager.ePagesType.Timetable)
                await ShowTimeTableAsync(tuple.Item2 as Timetable, false, false);
            else
            {
                var tuple2 = tuple.Item2 as Tuple<List<Lesson>, string>;

                Showplaces(tuple2.Item1, tuple2.Item2);
            }
            return true;
        }
        /// <summary>
        /// Loads plan
        /// </summary>
        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // load schooltimetable
            if (DataServices.IsFileExists())
            {
                InfoCenterStackPanel.Visibility = Visibility.Visible;
                InfoCenterProgressRing.Visibility = Visibility.Visible;
                InfoCenterButton.Visibility = Visibility.Collapsed;

                InfoCenterText.Text = "Trwa wczytywanie planu zajęć...";

                try
                {
                    TimeTable = await DataServices.Deserialize();
                }
                catch
                {
                    TimeTable = null;
                }

                if (TimeTable == null
                    || !TimeTable.TimetableOfTeachers.Any()
                    || !TimeTable.TimetablesOfClasses.Any())
                {
                    //removes settings
                    InfoCenterProgressRing.Visibility = Visibility.Collapsed;
                    TimeTable = new SchoolTimetable();
                    DownloadTimeTables("Wystąpił problem podczas wczytywania planu lekcji, muisz pobrać go od nowa. Chcesz to zrobić teraz?");
                    return;
                }

                MenuListViewOfSections.ItemsSource = TimeTableOfSections;
                MenuListViewOfTeachers.ItemsSource = TimeTableOfTeachers;

                InfoCenterProgressRing.Visibility = Visibility.Collapsed;

                //show last opened timetable, first check settings config
                var numOfClassesTimeTables = TimeTable.IdOfLastOpenedTimeTable;

                if (LocalSettingsServices.ShowTimetableAtStartup.ContainsKey())
                {
                    if ((int.Parse(LocalSettingsServices.ShowTimetableAtStartup.GetKeyValue()) == 0))
                        numOfClassesTimeTables = -1;
                    else
                        numOfClassesTimeTables = 1;
                }

                //if user dont want to show last timetable or no one are sets
                var lastTimetable = Timetable.GetLatestOpenedTimeTable(TimeTable);

                if (numOfClassesTimeTables == -1 || lastTimetable == null)
                {
                    InfoCenterText.Text = "Naciśnij przycisk menu u góry i wybierz interesujący Cię plan zajęć.";
                    InfoCenterButton.Visibility = Visibility.Collapsed;

                    _isLoaded = true;
                    return;
                }

                InfoCenterStackPanel.Visibility = Visibility.Collapsed;

                await ShowTimeTableAsync(lastTimetable, false, true);

                _isLoaded = true;
                return;
            }
            // if file doesnt exits
            DownloadTimeTables("By przeglądać plan zajęć, musiz go zsynchronizować, chcesz to zrobić teraz?");
        }

        private bool _isButtonClickEventSubscribed;
        private bool _isTimeTableDownloadedEventSubscribed;
        private bool _isAllTimeTablesDownloadedSubscribed;

        /// <summary>
        /// Downloads a plan
        /// </summary>
        /// <param name="textToShowAtInfoCenter">Text to show, before downloading</param>
        private void DownloadTimeTables(string textToShowAtInfoCenter)
        {
            SplitViewContentScrollViewer.Visibility = Visibility.Collapsed;
            InfoCenterStackPanel.Visibility = Visibility.Visible;
            InfoCenterButton.Visibility = Visibility.Visible;

            if (HtmlServices.UserHasInternetConnection())
            {
                InfoCenterText.Text = textToShowAtInfoCenter;
                _isLoaded = false;
            }
            else
                InfoCenterText.Text = "Aby odświeżyć plan zajęc, musisz mieć połączenie z internetem! Naciśnij przycisk poniżej aby spróbować ponownie";

            if (_isButtonClickEventSubscribed)
                return;

            _isButtonClickEventSubscribed = true;

            InfoCenterButton.Click += async (s, es) =>
            {
                //if user downloaded plan succesfully, but there was problem
                //with save and then clicked a button
                if (InfoCenterText.Text.Contains("NIE POWIODŁO SIĘ"))
                {
                    HtmlServices.InvokeAllTimeTableDownloaded();
                    return;
                }

                //check again if user has an internet connection
                //if not, call this function again to change text
                if (!HtmlServices.UserHasInternetConnection())
                {
                    DownloadTimeTables(textToShowAtInfoCenter);
                    return;
                }

                //if user wants to download a plan
                if (InfoCenterText.Text == textToShowAtInfoCenter
                    || !InfoCenterText.Text.Contains("zakończone"))
                {
                    InfoCenterButton.Visibility = Visibility.Collapsed;
                    InfoCenterProgressRing.Visibility = Visibility.Collapsed;

                    InfoCenterText.Text = "Trwa synchronizowanie planu...";

                    TimeTable.TimetableOfTeachers = new ObservableCollection<Timetable>();
                    TimeTable.TimetablesOfClasses = new ObservableCollection<Timetable>();

                    //we want to avoid the situations where OnTimeTableDownloaded event
                    //will be subscribed two times
                    if (!_isTimeTableDownloadedEventSubscribed)
                    {
                        _isTimeTableDownloadedEventSubscribed = true;

                        //called on each timetable downloaded to show progress
                        HtmlServices.OnTimeTableDownloaded += (timeTable, lenght) =>
                        {
                            var numOfTimeTable = TimeTable.TimetablesOfClasses.Count +
                                                 TimeTable.TimetableOfTeachers.Count();

                            if (timeTable.type == 0)
                                TimeTable.TimetablesOfClasses.Add(timeTable);
                            else
                                TimeTable.TimetableOfTeachers.Add(timeTable);

                            var percentOfDownloadedTimeTables = (int)(0.5f + (++numOfTimeTable * 100.0) / lenght);
                            InfoCenterText.Text = "[" + percentOfDownloadedTimeTables.ToString() + "%] Trwa dodawanie: " +
                                                  timeTable.name;
                        };
                    }

                    if (!_isAllTimeTablesDownloadedSubscribed)
                    {
                        _isAllTimeTablesDownloadedSubscribed = true;

                        HtmlServices.OnAllTimeTablesDownloaded += async () =>
                        {
                            InfoCenterText.Text = "Trwa zapisywanie planu zajęć...";

                            TimeTable.IdOfLastOpenedTimeTable = -1;

                            bool isPlanSerializedSuccesfullly = await DataServices.Serialize(TimeTable);

                            InfoCenterText.Text = !isPlanSerializedSuccesfullly
                                ? "Zapisywanie planu zajęć NIE POWIODŁO SIĘ. Spróbować ponownie?"
                                : "Synchronizowanie i zapisywanie planu zajęć zakończone.";

                            InfoCenterButton.Visibility = Visibility.Visible;
                            InfoCenterProgressRing.Visibility = Visibility.Collapsed;
                        };
                    }

                    //if user wants to download a plan but
                    //for he timetable is shown
                    if (_isLoaded)
                    {
                        InfoCenterStackPanel.Visibility = Visibility.Collapsed;
                        InfoCenterButton.Visibility = Visibility.Collapsed;
                        await ShowTimeTableAsync(Timetable.GetLatestOpenedTimeTable(TimeTable) ?? TimeTable.TimetablesOfClasses[0], false, false);
                    }

                    TimeTable = new SchoolTimetable();
                    await HtmlServices.GetData();
                }
                else
                { //if plan was downloaded&saved succesfully and user clicked OK button

                    _isLoaded = true;

                    InfoCenterStackPanel.Visibility = Visibility.Collapsed;

                    MenuListViewOfSections.ItemsSource = TimeTableOfSections;
                    MenuListViewOfTeachers.ItemsSource = TimeTableOfTeachers;

                    MenuSplitView.IsPaneOpen = true;
                }
            };
        }

        private bool _IsFlyOutHelperItemClickedSubscribed;

        /// <summary>
        /// Shows a timetable for user
        /// </summary>
        /// <param name="t">Timetable to show</param>
        /// <param name="quietChangedOfTimeTable">Changed timetable without showing it</param>
        /// <returns>Task for await</returns>
        private async Task ShowTimeTableAsync(Timetable t, bool quietChangedOfTimeTable = false, bool addPage = true)
        {
            if (InfoCenterStackPanel.Visibility == Visibility.Visible)
                InfoCenterStackPanel.Visibility = Visibility.Collapsed;

            // if we want to show timetable, without checking if eg settings page is opened
            if (!quietChangedOfTimeTable && SplitViewContentScrollViewer.Visibility == Visibility.Collapsed)
            {
                SplitViewContentScrollViewer.Visibility = Visibility.Visible;
                SplitViewContentFrame.Visibility = Visibility.Collapsed;
            }

            var splitViewContentGrid = MenuSplitViewContentGrid;

            //absolute id of timetable
            var idOfTimeTable = Timetable.GetIdOfTimetable(t, TimeTable);
            //t.type == Lesson.LessonType.Class ? TimeTable.TimetablesOfClasses.IndexOf(t) :
            //TimeTable.TimetablesOfClasses.Count + TimeTable.TimetableOfTeachers.IndexOf(t);

            //if table which we want to show is actually opened

            if (addPage)
                PagesManager.AddPage(t, PagesManager.ePagesType.Timetable);

            var headerGrid = splitViewContentGrid.Parent as Grid;

            if ((!quietChangedOfTimeTable && idOfTimeTable == TimeTable.IdOfLastOpenedTimeTable && splitViewContentGrid.Children.Any())
                || headerGrid == null)
            {
                if (!TitleText.Text.Contains("Plan lekcji"))
                {
                    TitleText.Text = "Plan lekcji - " + t.name;
                    return;
                }
            }
            var timeNow = DateTime.Now.TimeOfDay;
            var actualHour = timeNow.Hours;
            var actualMinute = timeNow.Minutes;

            //checks checking actual hour and minute which lesson actually is 
            var actualLesson = actualHour == 7 ? 1 : (actualHour == 8 && actualMinute < 55) ? 2 :
                ((actualHour == 8 && actualMinute >= 55) || actualHour == 9 && actualMinute < 45) ? 3 :
                ((actualHour == 9 && actualMinute >= 45) || actualHour == 10 && actualMinute < 45) ? 4 :
                ((actualHour == 10 && actualMinute >= 45) || actualHour == 11 && actualMinute < 35) ? 5 :
                ((actualHour == 11 && actualMinute >= 35) || actualHour == 12 && actualMinute < 30) ? 6 :
                ((actualHour == 12 && actualMinute >= 30) || actualHour == 13 && actualMinute < 20) ? 7 :
                ((actualHour == 13 && actualMinute >= 20) || actualHour == 14 && actualMinute < 10) ? 8 :
                ((actualHour == 14 && actualMinute >= 10)) ? 9 :
                ((actualHour == 15 && actualMinute >= 0) || actualHour == 15 && actualMinute < 50) ? 10 :
                ((actualHour == 15 && actualMinute >= 50) || actualHour >= 16) ? 11 : 0;

            if (!quietChangedOfTimeTable)
                TitleText.Text = "Plan lekcji - " + t.name;

            var actualTheme = Application.Current.RequestedTheme;

            //checks if we dont have title TextBlock created
            if (headerGrid.Children.FirstOrDefault(p => p is TextBlock && p != InfoCenterText) == null)
            {
                headerGrid.Children.Add(new TextBlock()
                {
                    Text = t.name,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(10.0),
                    Padding = new Thickness(10.0),
                    FontSize = 36
                });
            }
            else
                ((TextBlock)headerGrid.Children.First(p => p is TextBlock && p != InfoCenterText)).Text = t.name;

            //if we didnt created before a struct of grids
            //if we, then we have to delete it, lefts first row with
            //dayNames (poniedzialek,etc)
            if (!splitViewContentGrid.Children.Any())
            {
                for (var i = 0; i < 7; i++)
                {
                    splitViewContentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

                    var tx = new TextBlock()
                    {
                        Text = _dayNames[i],
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Padding = new Thickness(5.0)
                    };

                    var grid = new Grid();
                    grid.Children.Add(tx);
                    Grid.SetColumn(grid, i);

                    grid.BorderBrush = new SolidColorBrush(actualTheme == ApplicationTheme.Light ? Colors.Black : Colors.White);
                    grid.BorderThickness = new Thickness(1.0);
                    grid.Background = new SolidColorBrush(actualTheme == ApplicationTheme.Light ? Colors.LightCyan : Color.FromArgb(127, 0, 150, 0));

                    splitViewContentGrid.Children.Add(grid);
                }
            }
            else
            {
                splitViewContentGrid.RowDefinitions.Clear();

                var listOfObjects = splitViewContentGrid.Children.Select(p => (((Grid)p).Children[0] as TextBlock)).ToList();

                foreach (TextBlock tb in listOfObjects)
                {
                    if (!string.IsNullOrEmpty(_dayNames.FirstOrDefault(p => p.Contains(tb.Text))))
                        continue;

                    splitViewContentGrid.Children.Remove((tb.Parent as Grid));
                }
            }


            var numOfLessonsOnThisTimetable = t.days.Max(day => day.LessonsNum);

            //scans by rows
            for (var i = 0; i < numOfLessonsOnThisTimetable + 1; i++)
            {
                splitViewContentGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                // i=0 is a dayName eg Nr,Godz,Poniedzialek etc..., 
                //we dont want to show there lessons
                if (i == 0)
                    continue;

                //scans on every column
                for (var j = 0; j < 7; j++)
                {
                    var tx = new TextBlock();
                    var grid = new Grid
                    {
                        IsTapEnabled = true
                    };

                    //infoTextBlock provides a info about position lesson in a table
                    //for flyout lesson instance looking
                    var text = string.Empty;

                    //j=0 is a number of lesson
                    //j=1 is a hours of this lessons
                    //j>1 is a lesson
                    if (j == 0 || j == 1)
                    {
                        tx.HorizontalAlignment = HorizontalAlignment.Center;
                        tx.VerticalAlignment = VerticalAlignment.Center;
                    }

                    switch (j)
                    {
                        case 0:
                            text = i.ToString(); // number of lesson
                            grid.Background = new SolidColorBrush(actualTheme == ApplicationTheme.Light ? Colors.LightCyan : Color.FromArgb(127, 0, 150, 0));
                            break;

                        case 1:
                            text = _lessonTimes[i - 1]; // hour of lessons
                            grid.Background = new SolidColorBrush(actualTheme == ApplicationTheme.Light ? Colors.LightGreen : Color.FromArgb(127, 204, 0, 0));
                            break;

                        default:
                            var infoTextBlock = new TextBlock
                            {
                                Visibility = Visibility.Collapsed,
                                Text = $"[] {j} {i} {Timetable.GetIdOfTimetable(t, TimeTable)}"
                            };
                            grid.Children.Add(infoTextBlock);
                            //j is column, i is a row
                            //j - 2 because we have 2 added columns (Nr, Godz) 
                            //i - 1 because i=0 is a row with dayNames (Nr,Godz,Poniedzialek)etc..
                            var lesson = t.days[j - 2].Lessons[i - 1];

                            if (t.type == Lesson.LessonType.Teacher
                                && !string.IsNullOrEmpty(lesson.lesson2Name))
                            {

                                tx.Inlines.Add(new Run()
                                {
                                    Text = $"{lesson.lesson2Name} ",
                                    FontWeight = FontWeights.Light
                                });
                            }

                            tx.Inlines.Add(new Run() { Text = lesson.lesson1Name ?? "", FontWeight = FontWeights.Bold });

                            //if lesson1Tag (is a Teachertag) is not available, then
                            //skip it, else show full format
                            if (string.IsNullOrEmpty(lesson.lesson1Tag))
                            {
                                tx.Inlines.Add(new Run()
                                {
                                    Text = $" {lesson.lesson1Place}",
                                    Foreground = new SolidColorBrush(Colors.Red)
                                });
                            }
                            else
                            {
                                tx.Inlines.Add(new Run
                                {
                                    Text = $" {lesson.lesson1Tag}",
                                    Foreground = new SolidColorBrush(actualTheme == ApplicationTheme.Light ? Colors.Purple : Colors.LightCyan)
                                });
                                tx.Inlines.Add(new Run
                                {
                                    Text = $" {lesson.lesson1Place}",
                                    Foreground = new SolidColorBrush(Colors.Red)
                                });
                            }

                            //if this is a class timetable and
                            //at one time, we have two lessons then show
                            //seccond one at bottom in grid
                            if (!string.IsNullOrEmpty(lesson.lesson2Name)
                                && t.type == Lesson.LessonType.Class)
                            {

                                tx.Inlines.Add(new Run
                                {
                                    Text = $"{Environment.NewLine}{lesson.lesson2Name}",
                                    FontWeight = FontWeights.Bold
                                });
                                tx.Inlines.Add(new Run
                                {
                                    Text = $" {lesson.lesson2Tag}" ?? " ",
                                    Foreground = new SolidColorBrush(actualTheme == ApplicationTheme.Light ? Colors.Purple : Colors.LightCyan)
                                });
                                tx.Inlines.Add(new Run
                                {
                                    Text = $" {lesson.lesson2Place}" ?? " ",
                                    Foreground = new SolidColorBrush(Colors.Red)
                                });
                            }
                            break;
                    }

                    tx.Padding = new Thickness(10.0);

                    //if actually operated record
                    //was a lesson (then text was not added, and is empty)
                    if (text != "")
                        tx.Text = text;

                    if (tx.Text.Trim() != "" && j > 1)
                    {
                        grid.Tapped += (s, e) =>
                        {
                            var lesson = Lesson.GetLessonFromLessonGrid(s as Grid, TimeTable);

                            FlyoutHelper.SetTimetable(TimeTable);

                            //if lesson has two lesson, show flyout with choose which lesson
                            if (!string.IsNullOrEmpty(lesson.lesson2Tag))
                                FlyoutHelper.ShowFlyOutMenuForTwoLessons(grid, lesson);
                            else //clicked lesson has only one lesson
                                FlyoutHelper.ShowFlyOutMenuForLesson(grid);

                            if (_IsFlyOutHelperItemClickedSubscribed == false)
                            {
                                _IsFlyOutHelperItemClickedSubscribed = true;

                                FlyoutHelper.OnItemClicked += FlyoutHelper_OnItemClicked;
                            }
                        };
                    }
                    grid.Children.Add(tx);

                    Grid.SetColumn(grid, j);
                    Grid.SetRow(grid, i);

                    grid.BorderBrush = new SolidColorBrush(actualTheme == ApplicationTheme.Light ? Colors.Black : Colors.White);
                    grid.BorderThickness = new Thickness(1.0);

                    if (SettingsPage.IsShowActiveLessonsToogleSwitchOn() && i == actualLesson)
                    {
                        grid.BorderThickness = new Thickness(2);
                        grid.BorderBrush = new SolidColorBrush(actualTheme == ApplicationTheme.Light ? Colors.Red : Colors.Yellow);
                    }
                    splitViewContentGrid.Children.Add(grid);
                }
            }

            /* Saving lastOpenedTimeTable */
            if (await DataServices.SaveLastOpenedTimeTableToFile(idOfTimeTable, TimeTable) == false)
            {
                //If Plan is not saved
                ResetView();

                InfoCenterStackPanel.Visibility = Visibility.Visible;
                InfoCenterText.Visibility = Visibility.Visible;
                InfoCenterButton.Visibility = Visibility.Collapsed;

                InfoCenterText.Text =
                    "Wystąpił błąd podczas zapisu danych. Prawdopodobnie masz za mało pamięci na telefonie," +
                    " bądź inny błąd uniemożliwia zapis. Spróbuj uruchomić aplikację ponownie!";

                _isLoaded = false;
            }
        }

        /// <summary>
        /// When user clicked and selected an option on item
        /// </summary>
        /// <param name="clickedLesson">Clicked lesson</param>
        /// <param name="buttonType">Type of Button selected</param>
        /// <param name="lessonId">Number of lesson, which user want to execute</param>
        private async void FlyoutHelper_OnItemClicked(Lesson clickedLesson, FlyoutHelper.ButtonClickedType buttonType, int lessonId)
        {
            if (buttonType == FlyoutHelper.ButtonClickedType.BadButton)
                return;

            switch (buttonType)
            {
                case FlyoutHelper.ButtonClickedType.Place: //show me everything what's in this place
                    var place = lessonId == 0 ? clickedLesson.lesson1Place : clickedLesson.lesson2Place;

                    var listOfThingsInThisPlace = new List<Lesson>();

                    foreach (var timetable in TimeTable.TimetablesOfClasses)
                    {
                        foreach (var day in timetable.days)
                        {
                            foreach (var lesson in day.Lessons)
                            {
                                if ((lesson.lesson1Place != null && lesson.lesson1Place == place) || (lesson.lesson2Place != null && lesson.lesson2Place == place))
                                    listOfThingsInThisPlace.Add(lesson);
                            }
                        }
                    }

                    Showplaces(listOfThingsInThisPlace, place);
                    break;

                case FlyoutHelper.ButtonClickedType.Subject: //show me all subjects 
                    break;

                case FlyoutHelper.ButtonClickedType.Teacher: //show me teacher
                    var teacherName = lessonId == 0 ? clickedLesson.lesson1Tag : clickedLesson.lesson2Tag;

                    var timetableOfTeacher = TimeTableOfTeachers.FirstOrDefault(p => p.name.Substring(p.name.IndexOf('('),
                       p.name.IndexOf(p.name.ElementAt((p.name.Length - 1) - p.name.IndexOf('(')))).Contains(teacherName.Replace("#", "")));

                    if (timetableOfTeacher == null)
                        timetableOfTeacher = TimeTableOfTeachers.First(p => p.name.Contains("J.Pusiak"));

                    await ShowTimeTableAsync(timetableOfTeacher);
                    break;
                default:
                    return;
            }
        }

        private void Showplaces(List<Lesson> listOfLessonsInThisPlace, string place)
        {
            PagesManager.AddPage(new Tuple<List<Lesson>, string>(listOfLessonsInThisPlace, place), PagesManager.ePagesType.TimeTable_Place);

            var gird = MenuSplitViewContentGrid;
            var actualTheme = Application.Current.RequestedTheme;

            if (!gird.Children.Any())
            {
                for (var j = 0; j < 7; j++)
                {
                    gird.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

                    var tx = new TextBlock()
                    {
                        Text = _dayNames[j],
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Padding = new Thickness(5.0)
                    };

                    var grid = new Grid();
                    grid.Children.Add(tx);
                    Grid.SetColumn(grid, j);

                    grid.BorderBrush = new SolidColorBrush(actualTheme == ApplicationTheme.Light ? Colors.Black : Colors.White);
                    grid.BorderThickness = new Thickness(1.0);
                    grid.Background = new SolidColorBrush(actualTheme == ApplicationTheme.Light ? Colors.LightCyan : Color.FromArgb(127, 0, 150, 0));

                    gird.Children.Add(grid);
                }
            }
            else
            {
                gird.RowDefinitions.Clear();

                var listOfObjects = gird.Children.Select(p => (((Grid)p).Children[0] as TextBlock)).ToList();

                foreach (TextBlock tb in listOfObjects)
                {
                    if (!string.IsNullOrEmpty(_dayNames.FirstOrDefault(p => p.Contains(tb.Text))))
                        continue;

                    gird.Children.Remove((tb.Parent as Grid));
                }
            }

            SetTitleText($"Plan lekcji - Sala: {place}");

            var placeTimetable = new Timetable() { type = Lesson.LessonType.Class, name = place, days = new List<Day>() };

            var numOfLessonsOnThisPlace = listOfLessonsInThisPlace.Max(p => p.lessonDayPosition.lessonId)+1;

            placeTimetable.days.AddRange(Enumerable.Range(0, 5).Select( day => new Day() { Lessons = Enumerable.Range(0, numOfLessonsOnThisPlace).Select(
                lessonId => listOfLessonsInThisPlace.FirstOrDefault(lesson =>
                     lesson.lessonDayPosition.dayId == day && lesson.lessonDayPosition.lessonId == lessonId) ?? 
                     new Lesson() {lesson1Name = "", lessonDayPosition = new Lesson.dayCoordiantes() { dayId = day, lessonId = lessonId, timetableId = -1 } } ).ToList()}).ToList());


            var headerGrid = gird.Parent as Grid;

            //checks if we dont have title TextBlock created
            if (headerGrid.Children.FirstOrDefault(p => p is TextBlock && p != InfoCenterText) == null)
            {
                headerGrid.Children.Add(new TextBlock()
                {
                    Text = placeTimetable.name,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(10.0),
                    Padding = new Thickness(10.0),
                    FontSize = 36
                });
            }
            else
                ((TextBlock)headerGrid.Children.First(p => p is TextBlock && p != InfoCenterText)).Text = placeTimetable.name;


            //scans by rows
            for (var i = 0; i < numOfLessonsOnThisPlace+1; i++)
            {
                gird.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                // i=0 is a dayName eg Nr,Godz,Poniedzialek etc..., 
                //we dont want to show there lessons
                if (i == 0)
                    continue;

                //scans on every column
                for (var j = 0; j < 7; j++)
                {
                    var tx = new TextBlock();
                    var gridlesson = new Grid
                    {
                        IsTapEnabled = true
                    };

                    //infoTextBlock provides a info about position lesson in a table
                    //for flyout lesson instance looking
                    var text = string.Empty;

                    //j=0 is a number of lesson
                    //j=1 is a hours of this lessons
                    //j>1 is a lesson
                    if (j == 0 || j == 1)
                    {
                        tx.HorizontalAlignment = HorizontalAlignment.Center;
                        tx.VerticalAlignment = VerticalAlignment.Center;
                    }

                    switch (j)
                    {
                        case 0:
                            text = i.ToString(); // number of lesson
                            gridlesson.Background = new SolidColorBrush(actualTheme == ApplicationTheme.Light ? Colors.LightCyan : Color.FromArgb(127, 0, 150, 0));
                            break;

                        case 1:
                            text = _lessonTimes[i - 1]; // hour of lessons
                            gridlesson.Background = new SolidColorBrush(actualTheme == ApplicationTheme.Light ? Colors.LightGreen : Color.FromArgb(127, 204, 0, 0));
                            break;

                        default:
                            var infoTextBlock = new TextBlock
                            {
                                Visibility = Visibility.Collapsed,
                                Text = $"[] {j} {i} {Timetable.GetIdOfTimetable(placeTimetable.days[j - 2].Lessons[i - 1].lesson1Name == "" ? null : Timetable.GetTimetableById(placeTimetable.days[j - 2].Lessons[i - 1].lessonDayPosition.timetableId, TimeTable), TimeTable)}"
                            };

                            gridlesson.Children.Add(infoTextBlock);

                            var lesson = placeTimetable.days[j - 2].Lessons[i - 1];

                            var timetableId = lesson.lessonDayPosition.timetableId;
                            var timetableType = timetableId == -1 ? Lesson.LessonType.Class : Timetable.GetTimetableById(timetableId, TimeTable).type;
                            var timetable = timetableId == -1 ? null : Timetable.GetTimetableById(timetableId, TimeTable);

                            tx.Inlines.Add(new Run
                            {
                                Text = timetable == null ? "" : timetable.name ,
                                Foreground = new SolidColorBrush(actualTheme == ApplicationTheme.Light ? Colors.Purple : Colors.LightCyan)
                            });

                            tx.Inlines.Add(new Run
                            {
                                Text = place == lesson.lesson1Place ? $" {lesson.lesson1Name}" : $" {lesson.lesson2Name}",
                                Foreground = new SolidColorBrush(actualTheme == ApplicationTheme.Light ? Colors.Black : Colors.White),
                                FontWeight = FontWeights.Bold
                            });

                            tx.Inlines.Add(new Run
                            {
                                Text = place == lesson.lesson1Place ? $" {lesson.lesson1Tag}" : $" {lesson.lesson2Tag}",
                                Foreground = new SolidColorBrush(Colors.Red)
                            });

                            break;
                    }

                    tx.Padding = new Thickness(10.0);

                    //if actually operated record
                    //was a lesson (then text was not added, and is empty)
                    if (text != "")
                        tx.Text = text;

                    if (tx.Text.Trim() != "" && j > 1)
                    {
                        gridlesson.Tapped += (f, m) =>
                        {
                            var lesson = Lesson.GetLessonFromLessonGrid(f as Grid, TimeTable);

                            FlyoutHelper.SetTimetable(TimeTable);

                            FlyoutHelper.ShowFlyOutMenuForLesson(gridlesson,place==lesson.lesson1Place?0:1);

                            if (_IsFlyOutHelperItemClickedSubscribed == false)
                            {
                                _IsFlyOutHelperItemClickedSubscribed = true;

                                FlyoutHelper.OnItemClicked += FlyoutHelper_OnItemClicked;
                            }
                        };
                    }
                    gridlesson.Children.Add(tx);

                    Grid.SetColumn(gridlesson, j);
                    Grid.SetRow(gridlesson, i);

                    gridlesson.BorderBrush = new SolidColorBrush(actualTheme == ApplicationTheme.Light ? Colors.Black : Colors.White);
                    gridlesson.BorderThickness = new Thickness(1.0);

                    gird.Children.Add(gridlesson);
                }
            }
        }


        /// <summary>
        /// Reset view to default settings
        /// @Speccially removes all grids and sets title text to default
        /// <param name="titletext">title text to change</param>
        ///  </summary>
        private void ResetView(string titletext = "Plan lekcji")
        {
            MenuSplitViewContentGrid.Children.Clear();
            TitleText.Text = titletext;
        }

        /// <summary>
        /// Menu open click event
        /// </summary>
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoaded)
                MenuSplitView.IsPaneOpen = !MenuSplitView.IsPaneOpen;
        }
        /// <summary>
        /// Provides searching a teacher
        /// </summary>
        private void MenuListViewOfTeachersTextBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var text = sender.Text.ToLower();

            if (text.Trim() == string.Empty)
            {
                MenuListViewOfTeachers.ItemsSource = TimeTableOfTeachers;
                return;
            }
            MenuListViewOfTeachers.ItemsSource = TimeTableOfTeachers.Where(p => p.name.ToLower().Contains(text));
        }
        /// <summary>
        /// Sets visibility an focus of search AutoSuggestBox
        /// </summary>
        private void MenuListOfTeachersSearch_Button_Click(object sender, RoutedEventArgs e)
        {
            MenuListViewOfTeachersTextBox.Visibility = MenuListViewOfTeachersTextBox.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;

            if (MenuListViewOfTeachersTextBox.Visibility == Visibility.Visible)
                MenuListViewOfTeachersTextBox.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// Shows timetable when a any timetable was pressed
        /// </summary>
        private async void MenuListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (sender == MenuListViewOfSections)
                MenuListViewOfTeachers.SelectedIndex = -1;
            else
                MenuListViewOfSections.SelectedIndex = -1;

            await ShowTimeTableAsync(e.ClickedItem as Timetable);
        }

        /// <summary>
        /// Refresh timetable
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ResetView();
            DownloadTimeTables("Naciśnij przycisk OK, aby pobrać nowy plan zajęć.");
        }

        private bool _isOnHighLightActiveLessonsToogleSwitchSubscribed;

        /// <summary>
        /// Opens settings page
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoaded)
            {

                if (!_isOnHighLightActiveLessonsToogleSwitchSubscribed)
                {
                    _isOnHighLightActiveLessonsToogleSwitchSubscribed = true;

                    //If we changes an option of highligting active lesson
                    // in settings page, we have to refresh timetable
                    // with new settings
                    SettingsPage.OnHighLightActiveLessonsChanged += async () =>
                    {
                        //we have to use quiet change in timetable, which means
                        //that we dont want to change page and go with view to this timetable
                        //but we want to view stay in settings page, and change timetable 
                        //in background
                        await ShowTimeTableAsync(Timetable.GetLatestOpenedTimeTable(TimeTable), true, false);
                    };
                }

                //opens settings page
                SplitViewContentScrollViewer.Visibility = Visibility.Collapsed;
                SplitViewContentFrame.Visibility = Visibility.Visible;
                SplitViewContentFrame.Navigate(typeof(SettingsPage));
            }
        }
    }
}