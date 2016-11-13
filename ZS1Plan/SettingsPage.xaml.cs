using System;
using System.Linq;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace ZS1Plan
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public delegate void HighLightActiveLessonsChanged();
        public static event HighLightActiveLessonsChanged OnHighLightActiveLessonsChanged;

        private static SettingsPage _gui;

        public SettingsPage()
        {
            _gui = this;
            this.InitializeComponent();

            //befoe show user a view, we want to check if we must load
            //values to Toogle Switches
            CheckIfNightModeToogleSwitchShoudBeOn();
            CheckIfShowActiveLessonsToogleSwitchShouldBeOn();
            CheckIfShowTimetableAtStartupToogleSwitchShouldBeOn();

            //We are creating anonymous event handler in conscrutor of this class,
            //because we know, that this page will be created only one time, because
            //we added NavigationCacheMode="Required" which means, that the page will be
            //cached in memory
            NightModeToogleSwitch.Toggled += (s, e) =>
            {
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("AppTheme"))
                {
                    ApplicationData.Current.LocalSettings.Values.Remove("AppTheme");
                }

                ApplicationData.Current.LocalSettings.Values.Add("AppTheme",
                    Application.Current.RequestedTheme == ApplicationTheme.Dark ? ((int)ApplicationTheme.Light).ToString() : ((int)ApplicationTheme.Dark).ToString());

                //we are looking for a text block of this toogleswitch
                var headerTextBlock = ((TextBlock)NightModeToogleSwitch.Header);

                //if we changed text before.. dont add it again
                if (!headerTextBlock.Text.Contains("ponownym"))
                {
                    headerTextBlock.Text += Environment.NewLine +
                                                    "Zmiany zostaną wprowadzone po ponownym włączeniu aplikacji.";
                }
            };

            HighLightActualLessonToogleSwitch.Toggled += (s, e) =>
            {
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("ShowActiveLessons"))
                {
                    ApplicationData.Current.LocalSettings.Values.Remove("ShowActiveLessons");
                }

                ApplicationData.Current.LocalSettings.Values.Add("ShowActiveLessons",
                    HighLightActualLessonToogleSwitch.IsOn ? "1" : "0");

                //we have to call a MainPage, which have to quiet change
                //a timetable depends of this option above
                OnHighLightActiveLessonsChanged?.Invoke();
            };

            ShowTimeTableAtStartupToogleSwitch.Toggled += (s, e) =>
            {
                /*//If we changed a isOn from true to false, then
                //we have to only save a button value
                if (ShowTimeTableAtStartupToogleSwitch.IsOn == false)
                {
                    if (ApplicationData.Current.LocalSettings.Values.ContainsKey("ShowTimetableAtStartup"))
                    {
                        ApplicationData.Current.LocalSettings.Values.Remove("ShowTimetableAtStartup");
                    }

                    ApplicationData.Current.LocalSettings.Values.Add("ShowTimetableAtStartup",
                        ShowTimeTableAtStartupToogleSwitch.IsOn ? "1" : "0");
                }
                else// if we just changed IsOn from False to true, we have to
                {   //
                    if (ApplicationData.Current.LocalSettings.Values.ContainsKey("ShowTimetableAtStartupSelectedPlan"))
                    {
                        if ((string)ApplicationData.Current.LocalSettings.Values["ShowTimetableAtStartupSelectedPlan"] != "")
                        {
                            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("ShowTimetableAtStartup"))
                            {
                                ApplicationData.Current.LocalSettings.Values.Remove("ShowTimetableAtStartup");
                            }

                            ApplicationData.Current.LocalSettings.Values.Add("ShowTimetableAtStartup",
                                ShowTimeTableAtStartupToogleSwitch.IsOn ? "1" : "0");
                        }
                    }
                }*/

                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("ShowTimetableAtStartup"))
                {
                    ApplicationData.Current.LocalSettings.Values.Remove("ShowTimetableAtStartup");
                }

                ApplicationData.Current.LocalSettings.Values.Add("ShowTimetableAtStartup",
                    ShowTimeTableAtStartupToogleSwitch.IsOn ? "1" : "0");

                ShowTimeTableAtStartupComboBox.Visibility = ShowTimeTableAtStartupToogleSwitch.IsOn ? Visibility.Visible : Visibility.Collapsed;
            };
        }

        /// <summary>
        /// Sets a IsOn value to NightModeToogleSwitch
        /// </summary>
        private void CheckIfNightModeToogleSwitchShoudBeOn()
        {
            NightModeToogleSwitch.IsOn = ApplicationData.Current.LocalSettings.Values.ContainsKey("AppTheme")
                 && ((int.Parse((ApplicationData.Current.LocalSettings.Values["AppTheme"] as string))) == 1);
        }
        /// <summary>
        /// Sets a IsOn value to HighLightActualLessonToogleSwitch
        /// </summary>
        private void CheckIfShowActiveLessonsToogleSwitchShouldBeOn()
        {
            //if there isnt a value, then set a default value to 1
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("ShowActiveLessons"))
            {
                ApplicationData.Current.LocalSettings.Values.Add("ShowActiveLessons", "1");
            }
            HighLightActualLessonToogleSwitch.IsOn = ApplicationData.Current.LocalSettings.Values.ContainsKey("ShowActiveLessons") &&
                   (int.Parse(ApplicationData.Current.LocalSettings.Values["ShowActiveLessons"] as string) == 1);

        }

        public static bool IsShowActiveLessonsToogleSwitchOn()
        {
            return _gui?.HighLightActualLessonToogleSwitch.IsOn ??
                (ApplicationData.Current.LocalSettings.Values.ContainsKey("ShowActiveLessons") &&
                (int.Parse(ApplicationData.Current.LocalSettings.Values["ShowActiveLessons"] as string) == 1));
        }

        private void CheckIfShowTimetableAtStartupToogleSwitchShouldBeOn()
        {
            //sets default values
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("ShowTimetableAtStartup"))
            {
                ApplicationData.Current.LocalSettings.Values.Add("ShowTimetableAtStartup", "0");
            }
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("ShowTimetableAtStartupSelectedPlan"))
            {
                ApplicationData.Current.LocalSettings.Values.Add("ShowTimetableAtStartupSelectedPlan", "");
            }

            ShowTimeTableAtStartupToogleSwitch.IsOn = ApplicationData.Current.LocalSettings.Values.ContainsKey("ShowTimetableAtStartup") &&
                   (int.Parse(ApplicationData.Current.LocalSettings.Values["ShowTimetableAtStartup"] as string) == 1);

            //fill a ComboBox.Items with names of timetables
            foreach (var t in MainPage.TimeTable.GetAllTimeTables())
            {
                ShowTimeTableAtStartupComboBox.Items.Add(t.name);
            }

            //If switch is setted on, then we have to
            //set as selected item in ComboBox a selected
            //timetable
            if (ShowTimeTableAtStartupToogleSwitch.IsOn)
            {
                ShowTimeTableAtStartupComboBox.Visibility = Visibility.Visible;

                var nameOfSelectedItemInComboBox =
                    (string)ApplicationData.Current.LocalSettings.Values["ShowTimetableAtStartupSelectedPlan"];

                if (nameOfSelectedItemInComboBox == "" || ShowTimeTableAtStartupComboBox.Items == null)
                {
                    return;
                }

                int idOfSelectedItem = ShowTimeTableAtStartupComboBox.Items.IndexOf(nameOfSelectedItemInComboBox);

                if (idOfSelectedItem == -1)
                {
                    return;
                }

                var selectedItem = ShowTimeTableAtStartupComboBox.Items[idOfSelectedItem];

                ShowTimeTableAtStartupComboBox.SelectedItem = selectedItem;

                return;
            }

            ShowTimeTableAtStartupComboBox.Visibility = Visibility.Collapsed;

        }

        private bool _firstTimeSettingsPageOpened;
        private void ShowTimeTableAtStartupComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //we have to check if selection is changeg as first time, because
            //if we load the data, and then sets as selected some item
            //this function is called
            //and text appears. We dont want it
            if (!_firstTimeSettingsPageOpened)
            {
                _firstTimeSettingsPageOpened = true;
                return;
            }
            if (string.IsNullOrEmpty((string)ShowTimeTableAtStartupComboBox.SelectedItem))
            {
                MainPage.TimeTable.IdOfLastOpenedTimeTable = -1;
                return;
            }

            var headerTextBlock = ((TextBlock)ShowTimeTableAtStartupToogleSwitch.Header);

            if (!headerTextBlock.Text.Contains("aktualizacji"))
            {
                headerTextBlock.Text += Environment.NewLine +
                                        "Po każdej aktualizacji planu, będziesz musiał ustawić tę opcję ponownie.";
            }

            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("ShowTimetableAtStartup"))
            {
                ApplicationData.Current.LocalSettings.Values.Remove("ShowTimetableAtStartup");
            }

            ApplicationData.Current.LocalSettings.Values.Add("ShowTimetableAtStartup",
                ShowTimeTableAtStartupToogleSwitch.IsOn ? "1" : "0");

            var selectedTimeTable =
                MainPage.TimeTable.GetAllTimeTables()
                    .Find(p => p.name == (ShowTimeTableAtStartupComboBox.SelectedItem as string));

            ApplicationData.Current.LocalSettings.Values.Remove("ShowTimetableAtStartupSelectedPlan");
            ApplicationData.Current.LocalSettings.Values.Add("ShowTimetableAtStartupSelectedPlan", selectedTimeTable.name);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage.SetTitleText("Ustawienia");

            if (MainPage.InfoCenterStackPanelVisibility == Visibility.Visible)
            {
                MainPage.InfoCenterStackPanelVisibility = Visibility.Collapsed;
            }
        }
    }
}
