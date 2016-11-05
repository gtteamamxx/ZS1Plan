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
            // load

            //jesli nie ma
            InfoCenterStackPanel.Visibility = Visibility.Visible;
            InfoCenterText.Text = "By przeglądać plan lekcji, musiz go pobrać, chcesz go pobrać teraz?";

            timetable = await HTMLServices.getData();

            MenuListViewOfSections.ItemsSource = timetableOfSections;
            MenuListViewOfTeachers.ItemsSource = timetableOfTeachers;
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
    }
}
