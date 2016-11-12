﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace ZS1Plan
{
    public sealed partial class MainPage
    {
        public static SchoolTimetable TimeTable = new SchoolTimetable();

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
            get { return _gui.InfoCenterStackPanel.Visibility; }
            set { _gui.InfoCenterStackPanel.Visibility = value; }
        }

        public MainPage()
        {
            InitializeComponent();

            _gui = this;

            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                StatusBar.GetForCurrentView().ForegroundColor = Colors.White;
                StatusBar.GetForCurrentView().BackgroundOpacity = 1;
                StatusBar.GetForCurrentView().BackgroundColor = Colors.Black;
            }

            Loaded += MainPage_Loaded;


            if (ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
            {
                BackButton.Visibility = Visibility.Collapsed;

                Windows.Phone.UI.Input.HardwareButtons.BackPressed += (s, e) =>
                {
                    e.Handled = GoBack();
                };

                return;
            }

            BackButton.Click += (s, e) =>
            {
                if (!GoBack())
                {
                    Application.Current.Exit();
                }
            };
        }

        private bool GoBack()
        {
            if (SplitViewContentFrame.Visibility == Visibility.Visible
                && SplitViewContentFrame.SourcePageType == typeof(SettingsPage))
            {
                SplitViewContentScrollViewer.Visibility = Visibility.Visible;
                SplitViewContentFrame.Visibility = Visibility.Collapsed;

                TitleText.Text = "Plan lekcji" + (TimeTable.IdOfLastOpenedTimeTable == -1 ? "" : (" - " + TimeTable.GetLatestOpenedTimeTable().name));

                return true;
            }

            return false;
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

                TimeTable = await DataServices.Deserialize();

                if (!TimeTable.TimetableOfTeachers.Any() || !TimeTable.TimetablesOfClasses.Any())
                {
                    InfoCenterProgressRing.Visibility = Visibility.Collapsed;
                    TimeTable = new SchoolTimetable();
                    DownloadTimeTables("Wystąpił problem podczas wczytywania planu lekcji, muisz pobrać go od nowa. Chcesz to zrobić teraz?");
                    return;
                }

                MenuListViewOfSections.ItemsSource = TimeTableOfSections;
                MenuListViewOfTeachers.ItemsSource = TimeTableOfTeachers;

                InfoCenterProgressRing.Visibility = Visibility.Collapsed;

                var numOfClassesTimeTables = TimeTable.IdOfLastOpenedTimeTable;

                //show last opened
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("ShowTimetableAtStartup"))
                {
                    if ((int.Parse((string)ApplicationData.Current.LocalSettings.Values["ShowTimetableAtStartup"]) == 0))
                    {
                        numOfClassesTimeTables = -1;
                    }
                    else
                    {
                        numOfClassesTimeTables = 1;
                    }
                }

                if (numOfClassesTimeTables == -1)
                {
                    InfoCenterText.Text = "Naciśnij przycisk menu u góry i wybierz interesujący Cię plan zajęć.";
                    InfoCenterButton.Visibility = Visibility.Collapsed;

                    _isLoaded = true;
                    return;
                }

                InfoCenterStackPanel.Visibility = Visibility.Collapsed;

                ShowTimeTable(TimeTable.GetLatestOpenedTimeTable());

                _isLoaded = true;
                return;
            }

            DownloadTimeTables("By przeglądać plan zajęć, musiz go zsynchronizować, chcesz to zrobić teraz?");
        }

        private bool _isButtonClickEventSubscribed;
        private bool _isTimeTableDownloadedEventSubscribed;
        private bool _isAllTimeTablesDownloadedSubscribed;

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
            {
                InfoCenterText.Text = "Aby odświeżyć plan zajęc, musisz mieć połączenie z internetem! Naciśnij przycisk poniżej aby spróbować ponownie";
            }

            if (_isButtonClickEventSubscribed)
            {
                return;
            }
            else
            {
                _isButtonClickEventSubscribed = true;
            }

            InfoCenterButton.Click += async (s, es) =>
            {
                if (InfoCenterText.Text.Contains("NIE POWIODŁO SIĘ"))
                {
                    HtmlServices.InvokeAllTimeTableDownloaded();
                    return;
                }

                if (!HtmlServices.UserHasInternetConnection())
                {
                    DownloadTimeTables(textToShowAtInfoCenter);
                    return;
                }

                if (!InfoCenterText.Text.Contains("zakończone"))
                {
                    InfoCenterText.Text = textToShowAtInfoCenter;
                }

                if (InfoCenterText.Text == textToShowAtInfoCenter)
                {
                    InfoCenterButton.Visibility = Visibility.Collapsed;
                    InfoCenterProgressRing.Visibility = Visibility.Collapsed;

                    InfoCenterText.Text = "Trwa synchronizowanie planu...";

                    TimeTable.TimetableOfTeachers = new ObservableCollection<Timetable>();
                    TimeTable.TimetablesOfClasses = new ObservableCollection<Timetable>();

                    if (!_isTimeTableDownloadedEventSubscribed)
                    {
                        _isTimeTableDownloadedEventSubscribed = true;

                        HtmlServices.OnTimeTableDownloaded += (timeTable, lenght) =>
                        {
                            var numOfTimeTable = TimeTable.TimetablesOfClasses.Count +
                                                 TimeTable.TimetableOfTeachers.Count();

                            if (timeTable.type == 0)
                            {
                                TimeTable.TimetablesOfClasses.Add(timeTable);
                            }
                            else
                            {
                                TimeTable.TimetableOfTeachers.Add(timeTable);
                            }

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

                    if (_isLoaded)
                    {
                        InfoCenterStackPanel.Visibility = Visibility.Collapsed;
                        InfoCenterButton.Visibility = Visibility.Collapsed;
                        ShowTimeTable(TimeTable.GetLatestOpenedTimeTable() ?? TimeTable.TimetablesOfClasses[0]);
                    }

                    TimeTable = new SchoolTimetable();
                    await HtmlServices.GetData();
                }
                else
                {
                    _isLoaded = true;

                    InfoCenterStackPanel.Visibility = Visibility.Collapsed;

                    MenuListViewOfSections.ItemsSource = TimeTableOfSections;
                    MenuListViewOfTeachers.ItemsSource = TimeTableOfTeachers;

                    MenuSplitView.IsPaneOpen = true;
                }
            };
        }
        private async void ShowTimeTable(Timetable t, bool quietChangedOfTimeTable = false)
        {
            if (InfoCenterStackPanel.Visibility == Visibility.Visible)
            {
                InfoCenterStackPanel.Visibility = Visibility.Collapsed;
            }
            if (!quietChangedOfTimeTable && SplitViewContentScrollViewer.Visibility == Visibility.Collapsed)
            {
                SplitViewContentScrollViewer.Visibility = Visibility.Visible;
                SplitViewContentFrame.Visibility = Visibility.Collapsed;
            }

            var splitViewContentGrid = MenuSplitViewContentGrid;

            var idOfTimeTable = t.type == 0 ? TimeTable.TimetablesOfClasses.IndexOf(t) :
                TimeTable.TimetablesOfClasses.Count + TimeTable.TimetableOfTeachers.IndexOf(t);

            if (!quietChangedOfTimeTable && idOfTimeTable == TimeTable.IdOfLastOpenedTimeTable && splitViewContentGrid.Children.Any())
                return;

            var timeNow = DateTime.Now.TimeOfDay;
            var actualHour = timeNow.Hours;
            var actualMinute = timeNow.Minutes;

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

            var headerGrid = splitViewContentGrid.Parent as Grid;

            if (headerGrid == null)
            {
                return;
            }

            if (!quietChangedOfTimeTable)
                TitleText.Text = "Plan lekcji - " + t.name;

            var actualTheme = Application.Current.RequestedTheme;

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
            {
                ((TextBlock)headerGrid.Children.First(p => p is TextBlock && p != InfoCenterText)).Text = t.name;
            }

            if (!splitViewContentGrid.Children.Any())
            {
                for (var i = 0; i < 7; i++)
                {
                    splitViewContentGrid.ColumnDefinitions.Add(
                        new ColumnDefinition() { Width = GridLength.Auto });

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
                    {
                        continue;
                    }
                    splitViewContentGrid.Children.Remove((tb.Parent as Grid));
                }
            }

            var numOfRows = t.days.Max(day => day.lessonsNum);

            for (var i = 0; i < numOfRows + 1; i++)
            {
                splitViewContentGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                if (i == 0)
                {
                    continue;
                }

                for (var j = 0; j < 7; j++)
                {
                    var tx = new TextBlock();
                    var grid = new Grid();

                    var text = string.Empty;

                    if (j == 0 || j == 1)
                    {
                        tx.HorizontalAlignment = HorizontalAlignment.Center;
                        tx.VerticalAlignment = VerticalAlignment.Center;
                    }

                    switch (j)
                    {
                        case 0:
                            text = i.ToString();
                            grid.Background = new SolidColorBrush(actualTheme == ApplicationTheme.Light ? Colors.LightCyan : Color.FromArgb(127, 0, 150, 0));
                            break;

                        case 1:
                            text = _lessonTimes[i - 1];
                            grid.Background = new SolidColorBrush(actualTheme == ApplicationTheme.Light ? Colors.LightGreen : Color.FromArgb(127, 204, 0, 0));
                            break;

                        default:
                            var lesson = t.days[j - 2].Lessons[i - 1];

                            if (t.type == 1 && !string.IsNullOrEmpty(lesson.lesson2Name))
                            {
                                tx.Inlines.Add(new Run()
                                {
                                    Text = $"{lesson.lesson2Name} ",
                                    FontWeight = FontWeights.Light
                                });
                            }

                            tx.Inlines.Add((new Run() { Text = lesson.lesson1Name ?? " ", FontWeight = FontWeights.Bold }));

                            if (string.IsNullOrEmpty(lesson.lesson1Tag))
                            {
                                tx.Inlines.Add(new Run()
                                {
                                    Text = $" {lesson.lesson1Place}" ?? " ",
                                    Foreground = new SolidColorBrush(Colors.Red)
                                });
                            }
                            else
                            {
                                tx.Inlines.Add(new Run
                                {
                                    Text = $" {lesson.lesson1Tag}" ?? " ",
                                    Foreground = new SolidColorBrush(actualTheme == ApplicationTheme.Light ? Colors.Purple : Colors.LightCyan)
                                });
                                tx.Inlines.Add(new Run
                                {
                                    Text = $" {lesson.lesson1Place}" ?? " ",
                                    Foreground = new SolidColorBrush(Colors.Red)
                                });
                            }

                            if (!string.IsNullOrEmpty(lesson.lesson2Name) && t.type == 0)
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

                    if (text != "")
                        tx.Text = text;

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
            if (await SaveLastOpenedTimeTableToFile(idOfTimeTable) == false)
            {
                /* If Plan is not saved */
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

        private async Task<bool?> SaveLastOpenedTimeTableToFile(int idOfTimeTable)
        {
            if (idOfTimeTable == TimeTable.IdOfLastOpenedTimeTable)
            {
                return null;
            }

            TimeTable.IdOfLastOpenedTimeTable = idOfTimeTable;

            int numOfTriesToSave = 3;
            do
            {
            } while (!await DataServices.Serialize(TimeTable) && --numOfTriesToSave == 0);

            return numOfTriesToSave > 0;
        }

        private void ResetView()
        {
            MenuSplitViewContentGrid.Children.Clear();
            TextBlock tb = (((Grid)MenuSplitViewContentGrid.Parent).Children.FirstOrDefault(p => p is TextBlock) as TextBlock);

            TitleText.Text = "Plan lekcji";

            if (tb != null)
            {
                tb.Text = "";
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoaded)
            {
                MenuSplitView.IsPaneOpen = !MenuSplitView.IsPaneOpen;
            }
        }

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

        private void MenuListOfTeachersSearch_Button_Click(object sender, RoutedEventArgs e)
        {
            MenuListViewOfTeachersTextBox.Visibility = MenuListViewOfTeachersTextBox.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
            if (MenuListViewOfTeachersTextBox.Visibility == Visibility.Visible)
            {
                MenuListViewOfTeachersTextBox.Focus(FocusState.Programmatic);
            }
        }

        private void MenuListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ShowTimeTable(e.ClickedItem as Timetable);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ResetView();
            DownloadTimeTables("Naciśnij przycisk OK, aby pobrać nowy plan zajęć.");
        }

        private bool _isOnHighLightActiveLessonsToogleSwitchSubscribed;

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {

            if (_isLoaded)
            {
                if (!_isOnHighLightActiveLessonsToogleSwitchSubscribed)
                {
                    _isOnHighLightActiveLessonsToogleSwitchSubscribed = true;

                    SettingsPage.OnHighLightActiveLessonsChanged += () =>
                    {
                        ShowTimeTable(TimeTable.GetLatestOpenedTimeTable(), true);
                    };
                }

                SplitViewContentScrollViewer.Visibility = Visibility.Collapsed;
                SplitViewContentFrame.Visibility = Visibility.Visible;
                SplitViewContentFrame.Navigate(typeof(SettingsPage));
            }
        }
    }
}