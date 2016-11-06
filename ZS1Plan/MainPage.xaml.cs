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
        public static SchoolTimetable timetable = new SchoolTimetable();

        public ObservableCollection<Timetable> timetableOfSections => timetable.timetablesOfClasses;
        public ObservableCollection<Timetable> timetableOfTeachers => timetable.timetableOfTeachers;

        private bool isLoaded = false;

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

                timetable = await DataServices.Deserialize();

                MenuListViewOfSections.ItemsSource = timetableOfSections;
                MenuListViewOfTeachers.ItemsSource = timetableOfTeachers;

                InfoCenterProgressRing.Visibility = Visibility.Collapsed;

                //show last opened
                int numOfClassesTimeTables = timetable.idOfLastOpenedTimeTable;
                bool isReturnNeeded = false;

                // if -1 -> new timetable
                if (numOfClassesTimeTables == -1)
                {
                    InfoCenterText.Text = "Naciśnij przycisk menu u góry i wybierz interesujący Cię plan zajęć.";
                    InfoCenterButton.Visibility = Visibility.Collapsed;

                    isLoaded = true;
                    isReturnNeeded = true;
                }

                if (isReturnNeeded)
                    return;

                InfoCenterStackPanel.Visibility = Visibility.Collapsed;

                int idOfTimeTable;

                numOfClassesTimeTables = timetable.timetablesOfClasses.Count();

                int type = 0;
                if (timetable.idOfLastOpenedTimeTable < numOfClassesTimeTables)
                    idOfTimeTable = timetable.idOfLastOpenedTimeTable;
                else
                {
                    idOfTimeTable = timetable.idOfLastOpenedTimeTable - numOfClassesTimeTables;
                    type = 1;
                }

                ShowTimeTable(type == 0 ? timetable.timetablesOfClasses[idOfTimeTable] : timetable.timetableOfTeachers[idOfTimeTable]);

                isLoaded = true;
                return;
            }
            //jesli nie ma

            DownloadTimeTables("By przeglądać plan zajęć, musiz go zsynchronizować, chcesz to zrobić teraz?");
        }

        private void DownloadTimeTables(string textToShowAtInfoCenter)
        {
            isLoaded = false;

            InfoCenterStackPanel.Visibility = Visibility.Visible;
            InfoCenterButton.Visibility = Visibility.Visible;

            InfoCenterText.Text = textToShowAtInfoCenter;

            InfoCenterButton.Click += async (s, es) =>
            {
                if (InfoCenterText.Text[0] == textToShowAtInfoCenter[0])
                {
                    (s as Button).Visibility = Visibility.Collapsed;
                    InfoCenterProgressRing.Visibility = Visibility.Collapsed;

                    InfoCenterText.Text = "Trwa synchronizowanie planu...";

                    timetable.timetableOfTeachers = new ObservableCollection<Timetable>();
                    timetable.timetablesOfClasses = new ObservableCollection<Timetable>();

                    int numOfTimeTable = 0;
                    HTMLServices.OnTimeTableDownloaded += (timeTable, lenght) =>
                    {
                        if (timeTable.type == 0) timetable.timetablesOfClasses.Add(timeTable);
                        else timetable.timetableOfTeachers.Add(timeTable);

                        int percentOfDownloadedTimeTables = (int)(0.5f + (++numOfTimeTable * 100) / lenght);
                        InfoCenterText.Text = "[" + percentOfDownloadedTimeTables.ToString() + "%] Trwa dodawanie: " + timeTable.name;
                    };

                    HTMLServices.OnAllTimeTablesDownloaded += async () =>
                    {
                        InfoCenterText.Text = "Synchronizowanie planu zakończone. Trwa zapisywanie planu zajęć...";

                        timetable.idOfLastOpenedTimeTable = -1;
                        await DataServices.Serialize(timetable);

                        (s as Button).Visibility = Visibility.Visible;
                        InfoCenterProgressRing.Visibility = Visibility.Collapsed;

                        InfoCenterText.Text = "Synchronizowanie i zapisywanie planu zajęć zakończone.";
                    };

                    await HTMLServices.getData();
                }
                else
                {
                    isLoaded = true;

                    InfoCenterStackPanel.Visibility = Visibility.Collapsed;

                    MenuListViewOfSections.ItemsSource = timetableOfSections;
                    MenuListViewOfTeachers.ItemsSource = timetableOfTeachers;

                    MenuSplitView.IsPaneOpen = true;
                }
            };
        }
        private async void ShowTimeTable(Timetable t)
        {
            if (InfoCenterStackPanel.Visibility == Visibility.Visible)
                InfoCenterStackPanel.Visibility = Visibility.Collapsed;

            var SplitViewContentGrid = MenuSplitViewContentGrid;

            int idOfTimeTable = t.type == 0 ? timetable.timetablesOfClasses.IndexOf(t) :
                timetable.timetablesOfClasses.Count() + timetable.timetableOfTeachers.IndexOf(t);

            if (idOfTimeTable == timetable.idOfLastOpenedTimeTable && SplitViewContentGrid.Children.Count() > 0)
                return;

            string[] dayNames = { "Nr", "Godz", "Poniedziałek", "Wtorek", "Środa", "Czwartek",
                                  "Piątek" };

            string[] lessonTimes = { "7:10 - 7:55", "8:00 - 8:45", "8:50 - 9:35", "9:45 - 10:30",
                                   "10:45 - 11:30", "11:35 - 12:20", "12:30 - 13:15", "13:20 - 14:05",
                                    "14:10 - 14:55", "15:00 - 15:45", "15:50 - 16:35" };

            TimeSpan timeNow = DateTime.Now.TimeOfDay;
            int actualHour = timeNow.Hours;
            int actualMinute = timeNow.Minutes;

            int actualLesson = actualHour == 7 ? 1 : (actualHour == 8 && actualMinute < 55) ? 2 :
                ((actualHour == 8 && actualMinute >= 55) || actualHour == 9 && actualMinute < 45) ? 3 :
                ((actualHour == 9 && actualMinute >= 45) || actualHour == 10 && actualMinute < 45) ? 4 :
                ((actualHour == 10 && actualMinute >= 45) || actualHour == 11 && actualMinute < 35) ? 5 :
                ((actualHour == 11 && actualMinute >= 35) || actualHour == 12 && actualMinute < 30) ? 6 :
                ((actualHour == 12 && actualMinute >= 30) || actualHour == 13 && actualMinute < 20) ? 7 :
                ((actualHour == 13 && actualMinute >= 20) || actualHour == 14 && actualMinute < 10) ? 8 :
                ((actualHour == 14 && actualMinute >= 10) || actualHour == 15) ? 9 :
                ((actualHour == 15 && actualMinute >= 0) || actualHour == 15 && actualMinute < 50) ? 10 :
                ((actualHour == 15 && actualMinute >= 50) || actualHour >= 16) ? 11 : 0;

            var headerGrid = SplitViewContentGrid.Parent as Grid;

            if (headerGrid.Children.FirstOrDefault(p => p is TextBlock) == null)
                headerGrid.Children.Add(new TextBlock()
                {
                    Text = t.name,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(10.0),
                    Padding = new Thickness(10.0),
                    FontSize = 36
                });
            else
                (headerGrid.Children.First(p => p is TextBlock) as TextBlock).Text = t.name;

            if (SplitViewContentGrid.Children.Count() == 0)
            {
                for (int i = 0; i < 7; i++)
                {
                    ColumnDefinition cd = new ColumnDefinition();
                    cd.Width = GridLength.Auto;

                    SplitViewContentGrid.ColumnDefinitions.Add(cd);

                    TextBlock tx = new TextBlock();
                    tx.Text = dayNames[i];
                    tx.HorizontalAlignment = HorizontalAlignment.Center;
                    tx.Padding = new Thickness(5.0);

                    Grid grid = new Grid();
                    grid.Children.Add(tx);
                    Grid.SetColumn(grid, i);

                    grid.BorderBrush = new SolidColorBrush(Colors.Black);
                    grid.BorderThickness = new Thickness(1.0);
                    grid.Background = new SolidColorBrush(Colors.LightCyan);

                    SplitViewContentGrid.Children.Add(grid);
                }
            }
            else
            {
                SplitViewContentGrid.RowDefinitions.Clear();

                var listOfObjects = SplitViewContentGrid.Children.Select(p => ((p as Grid).Children[0] as TextBlock)).ToList();

                foreach (var tb in listOfObjects)
                {
                    if (!string.IsNullOrEmpty(dayNames.FirstOrDefault(p => p.Contains(tb.Text))))
                        continue;
                    SplitViewContentGrid.Children.Remove((tb.Parent as Grid));
                }
            }

            int numOfRows = 0;
            foreach (var day in t.days)
                if (day.lessonsNum > numOfRows)
                    numOfRows = day.lessonsNum;

            for (int i = 0; i < numOfRows + 1; i++)
            {
                RowDefinition rd = new RowDefinition();
                rd.Height = GridLength.Auto;

                SplitViewContentGrid.RowDefinitions.Add(rd);

                if (i != 0)
                {
                    for (int j = 0; j < 7; j++)
                    {
                        TextBlock tx = new TextBlock();
                        Grid grid = new Grid();

                        string text = string.Empty;

                        if (j == 0 || j == 1)
                        {
                            tx.HorizontalAlignment = HorizontalAlignment.Center;
                            tx.VerticalAlignment = VerticalAlignment.Center;
                        }

                        if (j == 0)
                        {
                            text = i.ToString();
                            grid.Background = new SolidColorBrush(Colors.LightCyan);
                        }
                        else if (j == 1)
                        {
                            text = lessonTimes[i - 1];
                            grid.Background = new SolidColorBrush(Colors.LightGreen);
                        }
                        else
                        {
                            var lesson = t.days[j - 2].Lessons[i - 1];

                            if (t.type == 1 && !string.IsNullOrEmpty(lesson.lesson2Name))
                                tx.Inlines.Add(new Run() { Text = $"{lesson.lesson2Name} ", FontWeight = FontWeights.Light });

                            tx.Inlines.Add((new Run() { Text = lesson.lesson1Name ?? " ", FontWeight = FontWeights.Bold }));

                            if (string.IsNullOrEmpty(lesson.lesson1Tag))
                                tx.Inlines.Add(new Run() { Text = $" {lesson.lesson1Place}" ?? " ", Foreground = new SolidColorBrush(Colors.Red) });
                            else
                            {
                                tx.Inlines.Add(new Run() { Text = $" {lesson.lesson1Tag}" ?? " ", Foreground = new SolidColorBrush(Colors.Purple) });
                                tx.Inlines.Add(new Run() { Text = $" {lesson.lesson1Place}" ?? " ", Foreground = new SolidColorBrush(Colors.Red) });
                            }

                            if (!string.IsNullOrEmpty(lesson.lesson2Name) && t.type == 0)
                            {
                                tx.Inlines.Add(new Run() { Text = $"{Environment.NewLine}{lesson.lesson2Name}", FontWeight = FontWeights.Bold });
                                tx.Inlines.Add(new Run() { Text = $" {lesson.lesson2Tag}" ?? " ", Foreground = new SolidColorBrush(Colors.Purple) });
                                tx.Inlines.Add(new Run() { Text = $" {lesson.lesson2Place}" ?? " ", Foreground = new SolidColorBrush(Colors.Red) });
                            }
                        }

                        tx.Padding = new Thickness(10.0);

                        if (text != "")
                            tx.Text = text;

                        grid.Children.Add(tx);

                        Grid.SetColumn(grid, j);
                        Grid.SetRow(grid, i);

                        grid.BorderBrush = new SolidColorBrush(Colors.Black);
                        grid.BorderThickness = new Thickness(1.0);

                        if (i == actualLesson)
                        {
                            grid.BorderThickness = new Thickness(2);
                            grid.BorderBrush = new SolidColorBrush(Colors.Red);
                        }
                        SplitViewContentGrid.Children.Add(grid);
                    }
                }
            }

            if (idOfTimeTable != timetable.idOfLastOpenedTimeTable)
            {
                timetable.idOfLastOpenedTimeTable = idOfTimeTable;
                await DataServices.Serialize(timetable);
            }
        }
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
                MenuSplitView.IsPaneOpen = !MenuSplitView.IsPaneOpen;
        }

        private void MenuListViewOfTeachersTextBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var text = sender.Text.ToLower();

            if (text.Trim() == string.Empty)
            {
                MenuListViewOfTeachers.ItemsSource = timetableOfTeachers;
                return;
            }
            MenuListViewOfTeachers.ItemsSource = timetableOfTeachers.Where(p => p.name.ToLower().Contains(text));
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
            MenuSplitViewContentGrid.Children.Clear();
            var tb = ((MenuSplitViewContentGrid.Parent as Grid).Children.FirstOrDefault(p => p is TextBlock) as TextBlock);

            if (tb != null)
                tb.Text = "";

            DownloadTimeTables("Naciśnij przycisk OK, aby pobrać nowy plan zajęć.");
        }
    }
}
