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
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
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
            InfoCenterStackPanel.Visibility = Visibility.Visible;
            // load
            if (DataServices.IsFileExists())
            {
                InfoCenterText.Text = "Trwa wczytywanie planu lekcji...";
                InfoCenterProgressRing.Visibility = Visibility.Visible;

                timetable = await DataServices.Deserialize();

                MenuListViewOfSections.ItemsSource = timetableOfSections;
                MenuListViewOfTeachers.ItemsSource = timetableOfTeachers;

                InfoCenterProgressRing.Visibility = Visibility.Collapsed;

                //show last opened
                int numOfClassesTimeTables = timetable.timetablesOfClasses.Count();
                bool isReturnNeeded = false;

                // -1 -> new timetable
                if (numOfClassesTimeTables == -1)
                {
                    InfoCenterText.Text = "Naciśnij przycisk menu u góry i wybierz ineresujący Cię plan zajęc.";
                    isLoaded = true;
                    isReturnNeeded = true;
                }

                if (isReturnNeeded)
                    return;

                InfoCenterStackPanel.Visibility = Visibility.Collapsed;

                int idOfTimeTable;

                idOfTimeTable = timetable.idOfLastOpenedTimeTable >= numOfClassesTimeTables ? 
                    timetable.idOfLastOpenedTimeTable - numOfClassesTimeTables : 
                    numOfClassesTimeTables;

                //show

                isLoaded = true;
                return;
            }
            //jesli nie ma
            InfoCenterText.Text = "By przeglądać plan lekcji, musiz go zsynchronizować, chcesz to zrobić teraz?";
            InfoCenterButton.Click += async (s, es) =>
            {
                if (InfoCenterText.Text[0] == 'B')
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
                        InfoCenterText.Text = "["+ percentOfDownloadedTimeTables.ToString()+ "%] Trwa dodawanie: " + timeTable.name;
                    };

                    HTMLServices.OnAllTimeTablesDownloaded += async () =>
                    {
                        InfoCenterText.Text = "Synchronizowanie planu zakończone. Trwa zapisywanie planu lekcji...";

                        timetable.idOfLastOpenedTimeTable = -1;
                        await DataServices.Serialize(timetable);

                        (s as Button).Visibility = Visibility.Visible;
                        InfoCenterProgressRing.Visibility = Visibility.Collapsed;

                        InfoCenterText.Text = "Synchronizowanie i zapisywanie planu lekcji zakończone.";
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

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if(isLoaded) 
                MenuSplitView.IsPaneOpen = !MenuSplitView.IsPaneOpen;
        }
        
        private void MenuListViewOfTeachersTextBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var text = sender.Text.ToLower();

            if(text.Trim() == string.Empty)
            {
                MenuListViewOfTeachers.ItemsSource = timetableOfTeachers;
                return;
            }
            MenuListViewOfTeachers.ItemsSource = timetableOfTeachers.Where(p => p.name.ToLower().Contains(text));
        }

        private void MenuListOfTeachersSearchButton_Click(object sender, RoutedEventArgs e)
        {
            MenuListViewOfTeachersTextBox.Visibility = MenuListViewOfTeachersTextBox.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
            if (MenuListViewOfTeachersTextBox.Visibility == Visibility.Visible) MenuListViewOfTeachersTextBox.Focus(FocusState.Programmatic);
        }
    }
}
