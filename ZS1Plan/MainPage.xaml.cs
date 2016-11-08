using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ZS1Plan
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static SchoolTimetable MainSchoolTimetable = new SchoolTimetable();

        public ObservableCollection<Timetable> TimetableOfSections => MainSchoolTimetable.timetablesOfClasses;
        public ObservableCollection<Timetable> TimetableOfTeachers => MainSchoolTimetable.timetableOfTeachers;

        private bool _isLoaded = false;

        public MainPage()
        {
            this.InitializeComponent();

            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                StatusBar.GetForCurrentView().ForegroundColor = Colors.White;
                StatusBar.GetForCurrentView().BackgroundOpacity = 1;
                StatusBar.GetForCurrentView().BackgroundColor = Colors.Black;
            }
            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // load
            if (DataServices.IsFileExists())
            {
                InfoCenterStackPanel.Visibility = Visibility.Visible;
                InfoCenterProgressRing.Visibility = Visibility.Visible;
                InfoCenterButton.Visibility = Visibility.Collapsed;

                InfoCenterText.Text = "Trwa wczytywanie planu zajęć...";

                MainSchoolTimetable = await DataServices.Deserialize();

                MenuListViewOfSections.ItemsSource = TimetableOfSections;
                MenuListViewOfTeachers.ItemsSource = TimetableOfTeachers;

                InfoCenterProgressRing.Visibility = Visibility.Collapsed;

                //show last opened
                int numOfClassesTimeTables = MainSchoolTimetable.idOfLastOpenedTimeTable;
                bool isReturnNeeded = false;

                // if -1 -> new timetable
                if (numOfClassesTimeTables == -1)
                {
                    InfoCenterText.Text = "Naciśnij przycisk menu u góry i wybierz interesujący Cię plan zajęć.";
                    InfoCenterButton.Visibility = Visibility.Collapsed;

                    _isLoaded = true;
                    isReturnNeeded = true;
                }

                if (isReturnNeeded)
                    return;

                InfoCenterStackPanel.Visibility = Visibility.Collapsed;

                int idOfTimeTable;

                numOfClassesTimeTables = MainSchoolTimetable.timetablesOfClasses.Count();

                var type = 0;

                if (MainSchoolTimetable.idOfLastOpenedTimeTable < numOfClassesTimeTables)
                    idOfTimeTable = MainSchoolTimetable.idOfLastOpenedTimeTable;
                else
                {
                    idOfTimeTable = MainSchoolTimetable.idOfLastOpenedTimeTable - numOfClassesTimeTables;
                    type = 1;
                }

                ShowTimeTable(type == 0 ? MainSchoolTimetable.timetablesOfClasses[idOfTimeTable] : MainSchoolTimetable.timetableOfTeachers[idOfTimeTable]);

                _isLoaded = true;
                return;
            }
            //jesli nie ma

            DownloadTimeTables("By przeglądać plan zajęć, musiz go zsynchronizować, chcesz to zrobić teraz?");
        }

        private void DownloadTimeTables(string textToShowAtInfoCenter)
        {
            _isLoaded = false;

            InfoCenterStackPanel.Visibility = Visibility.Visible;
            InfoCenterButton.Visibility = Visibility.Visible;

            InfoCenterText.Text = textToShowAtInfoCenter;

            InfoCenterButton.Click += async (s, es) =>
            {
                if (InfoCenterText.Text[0] == textToShowAtInfoCenter[0])
                {
                    ((Button)s).Visibility = Visibility.Collapsed;
                    InfoCenterProgressRing.Visibility = Visibility.Collapsed;

                    InfoCenterText.Text = "Trwa synchronizowanie planu...";

                    MainSchoolTimetable.timetableOfTeachers = new ObservableCollection<Timetable>();
                    MainSchoolTimetable.timetablesOfClasses = new ObservableCollection<Timetable>();

                    int numOfTimeTable = 0;
                    HTMLServices.OnTimeTableDownloaded += (timeTable, lenght) =>
                    {
                        if (timeTable.type == 0) MainSchoolTimetable.timetablesOfClasses.Add(timeTable);
                        else MainSchoolTimetable.timetableOfTeachers.Add(timeTable);

                        var percentOfDownloadedTimeTables = (int)(0.5f + (++numOfTimeTable * 100.0) / lenght);
                        InfoCenterText.Text = "[" + percentOfDownloadedTimeTables.ToString() + "%] Trwa dodawanie: " + timeTable.name;
                    };

                    HTMLServices.OnAllTimeTablesDownloaded += async () =>
                    {
                        InfoCenterText.Text = "Synchronizowanie planu zakończone. Trwa zapisywanie planu zajęć...";

                        MainSchoolTimetable.idOfLastOpenedTimeTable = -1;
                        await DataServices.Serialize(MainSchoolTimetable);

                        ((Button)s).Visibility = Visibility.Visible;
                        InfoCenterProgressRing.Visibility = Visibility.Collapsed;

                        InfoCenterText.Text = "Synchronizowanie i zapisywanie planu zajęć zakończone.";
                    };

                    await HTMLServices.getData();
                }
                else
                {
                    _isLoaded = true;

                    InfoCenterStackPanel.Visibility = Visibility.Collapsed;

                    MenuListViewOfSections.ItemsSource = TimetableOfSections;
                    MenuListViewOfTeachers.ItemsSource = TimetableOfTeachers;

                    MenuSplitView.IsPaneOpen = true;
                }
            };
        }
        private async void ShowTimeTable(Timetable t)
        {
            var splitViewContentGrid = MenuSplitViewContentGrid;

            var headerGrid = splitViewContentGrid.Parent as Grid;

            if (headerGrid == null)
            {
                return;
            }

            var idOfTimeTable = t.type == 0 ? MainSchoolTimetable.timetablesOfClasses.IndexOf(t) :
                MainSchoolTimetable.timetablesOfClasses.Count() + MainSchoolTimetable.timetableOfTeachers.IndexOf(t);

            if (idOfTimeTable == MainSchoolTimetable.idOfLastOpenedTimeTable && splitViewContentGrid.Children.Any())
            {
                return;
            }

            if (InfoCenterStackPanel.Visibility == Visibility.Visible)
            {
                InfoCenterStackPanel.Visibility = Visibility.Collapsed;
            }

            var SplitViewContentGrid = MenuSplitViewContentGrid;

            string[] dayNames = { "Nr", "Godz", "Poniedziałek", "Wtorek", "Środa", "Czwartek",
                                  "Piątek" };

            string[] lessonTimes = { "7:10 - 7:55", "8:00 - 8:45", "8:50 - 9:35", "9:45 - 10:30",
                                   "10:45 - 11:30", "11:35 - 12:20", "12:30 - 13:15", "13:20 - 14:05",
                                    "14:10 - 14:55", "15:00 - 15:45", "15:50 - 16:35"};

            var timeNow = DateTime.Now.TimeOfDay;
            var actualHour = timeNow.Hours;
            var actualMinute = timeNow.Minutes;

            var actualLesson = actualHour == 7 ? 1 : (actualHour == 8 && actualMinute < 45) ? 2 :
                ((actualHour == 8 && actualMinute >= 45) || actualHour == 9 && actualMinute < 35) ? 3 :
                ((actualHour == 9 && actualMinute >= 35) || actualHour == 10 && actualMinute < 30) ? 4 :
                ((actualHour == 10 && actualMinute >= 30) || actualHour == 11 && actualMinute < 30) ? 5 :
                ((actualHour == 11 && actualMinute >= 30) || actualHour == 12 && actualMinute < 20) ? 6 :
                ((actualHour == 12 && actualMinute >= 20) || actualHour == 13 && actualMinute < 15) ? 7 :
                ((actualHour == 13 && actualMinute >= 15) || actualHour == 14 && actualMinute < 5) ? 8 :
                ((actualHour == 14 && actualMinute >= 5) || (actualHour == 14 && actualMinute < 55)) ? 9 :
                ((actualHour == 14 && actualMinute >= 55) || actualHour == 15 || (actualHour == 15 && actualMinute < 45)) ? 10 :
                ((actualHour == 15 && actualMinute >= 45) || actualHour >= 16) ? 11 : 0;

            var createHeadTextBlock = false;

            if (headerGrid.Children.FirstOrDefault(p => p is TextBlock) == null)
            {
                createHeadTextBlock = true;
            }
            else
            {
                var headTextBlock = (headerGrid.Children.First(p => p is TextBlock) as TextBlock);

                if (headTextBlock == null)
                {
                    createHeadTextBlock = true;
                }
                else
                {
                    headTextBlock.Text = t.name;
                }
            }

            if (createHeadTextBlock)
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

            if (splitViewContentGrid.Children.Any())
            {
                for (var i = 0; i < 7; i++)
                {
                    var cd = new ColumnDefinition()
                    {
                        Width = GridLength.Auto
                    };

                    splitViewContentGrid.ColumnDefinitions.Add(cd);

                    TextBlock tx = new TextBlock();
                    tx.Text = dayNames[i];
                    tx.HorizontalAlignment = HorizontalAlignment.Center;
                    tx.Padding = new Thickness(10.0);

                    var grid = new Grid();
                    grid.Children.Add(tx);
                    Grid.SetColumn(grid, i);

                    grid.BorderBrush = new SolidColorBrush(Colors.Black);
                    grid.BorderThickness = new Thickness(1.0);
                    grid.Background = new SolidColorBrush(Colors.LightCyan);

                    splitViewContentGrid.Children.Add(grid);
                }
            }
            else
            {
                splitViewContentGrid.RowDefinitions.Clear();

                var listOfObjects = splitViewContentGrid.Children.Select(p => ((p as Grid).Children[0] as TextBlock)).ToList();

                foreach (var tb in listOfObjects)
                {
                    if (!string.IsNullOrEmpty(dayNames.FirstOrDefault(p => p.Contains(tb.Text))))
                        continue;
                    splitViewContentGrid.Children.Remove((tb.Parent as Grid));
                }
            }

            var numOfRows = t.days.Select(day => day.lessonsNum).Max();

            for (var i = 0; i < numOfRows + 1; i++)
            {
                var rd = new RowDefinition()
                {
                    Height = GridLength.Auto
                };

                splitViewContentGrid.RowDefinitions.Add(rd);

                if (i == 0)
                {
                    for (int j = 0; j < 7; j++)
                    {
                        TextBlock tx = new TextBlock();

                    if (j == 0 || j == 1)
                    {
                        tx.HorizontalAlignment = HorizontalAlignment.Center;
                        tx.VerticalAlignment = VerticalAlignment.Center;
                    }

                        if (j == 0)
                        {
                            text = i.ToString();
                            tx.HorizontalAlignment = HorizontalAlignment.Center;
                        }
                        else if (j == 1)
                        {
                            text = lessonTimes[i - 1];
                            tx.HorizontalAlignment = HorizontalAlignment.Center;
                        }
                        else
                        {
                            var lesson = t.days[j - 2].Lessons[i - 1];
                            text = lesson.lesson1Name;

                            if (!string.IsNullOrEmpty(lesson.lesson2Name))
                                text += Environment.NewLine + lesson.lesson2Name;
                        }

                        tx.Text = text == null ? "" : text;
                        tx.Padding = new Thickness(10.0);

                        Grid grid = new Grid();
                        grid.Children.Add(tx);

                    Grid.SetColumn(grid, j);
                    Grid.SetRow(grid, i);

                        grid.BorderBrush = new SolidColorBrush(Colors.Black);
                        grid.BorderThickness = new Thickness(1.0);
                        SplitViewContentGrid.Children.Add(grid);
                    }
                    splitViewContentGrid.Children.Add(grid);
                }
            }

            int idOfTimeTable = t.type == 0 ? timetable.timetablesOfClasses.IndexOf(t) :
                timetable.timetablesOfClasses.Count() + timetable.timetableOfTeachers.IndexOf(t);

            if (idOfTimeTable != timetable.idOfLastOpenedTimeTable)
            {
                MainSchoolTimetable.idOfLastOpenedTimeTable = idOfTimeTable;
                await DataServices.Serialize(MainSchoolTimetable);
            }
        }
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoaded)
                MenuSplitView.IsPaneOpen = !MenuSplitView.IsPaneOpen;
        }

        private void MenuListViewOfTeachersTextBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var text = sender.Text.ToLower();

            if (text.Trim() == string.Empty)
            {
                MenuListViewOfTeachers.ItemsSource = TimetableOfTeachers;
                return;
            }
            MenuListViewOfTeachers.ItemsSource = TimetableOfTeachers.Where(p => p.name.ToLower().Contains(text));
        }

        private void MenuListOfTeachersSearch_Button_Click(object sender, RoutedEventArgs e)
        {
            MenuListViewOfTeachersTextBox.Visibility = MenuListViewOfTeachersTextBox.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
            if (MenuListViewOfTeachersTextBox.Visibility == Visibility.Visible) MenuListViewOfTeachersTextBox.Focus(FocusState.Programmatic);
        }

        private void MenuListView_ItemClick(object sender, ItemClickEventArgs e)
            => ShowTimeTable(e.ClickedItem as Timetable);

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            var mainGrid = (MenuSplitViewContentGrid.Parent as Grid);

            if (mainGrid == null)
            {
                return;
            }

            MenuSplitViewContentGrid.Children.Clear();
            var tb = (mainGrid.Children.FirstOrDefault(p => p is TextBlock) as TextBlock);

            if (tb != null)
                tb.Text = "";

            DownloadTimeTables("Naciśnij przycisk OK, aby pobrać nowy plan zajęć.");
        }
    }
}
