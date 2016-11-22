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
                if (LocalSettingsServices.AppTheme.ContainsKey())
                {
                    LocalSettingsServices.AppTheme.RemoveKey();
                }

                LocalSettingsServices.AppTheme.AddKey(Application.Current.RequestedTheme);

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
                if (LocalSettingsServices.ShowActiveLesson.ContainsKey())
                {
                    LocalSettingsServices.ShowActiveLesson.RemoveKey();
                }

                LocalSettingsServices.ShowActiveLesson.AddKey(HighLightActualLessonToogleSwitch.IsOn);

                //we have to call a MainPage, which have to quiet change
                //a timetable depends of this option above
                OnHighLightActiveLessonsChanged?.Invoke();
            };

            ShowTimeTableAtStartupToogleSwitch.Toggled += (s, e) =>
            {
                if (LocalSettingsServices.ShowTimetableAtStartup.ContainsKey())
                {
                    LocalSettingsServices.ShowTimetableAtStartup.RemoveKey();
                }

                LocalSettingsServices.ShowTimetableAtStartup.AddKey(HighLightActualLessonToogleSwitch.IsOn);


                ShowTimeTableAtStartupComboBox.Visibility = ShowTimeTableAtStartupToogleSwitch.IsOn ? Visibility.Visible : Visibility.Collapsed;
            };
        }

        /// <summary>
        /// Sets a IsOn value to NightModeToogleSwitch
        /// </summary>
        private void CheckIfNightModeToogleSwitchShoudBeOn()
        {
            NightModeToogleSwitch.IsOn = LocalSettingsServices.AppTheme.ContainsKey()
                 && (int.Parse(LocalSettingsServices.AppTheme.GetKeyValue()) == 1);
        }
        /// <summary>
        /// Sets a IsOn value to HighLightActualLessonToogleSwitch
        /// </summary>
        private void CheckIfShowActiveLessonsToogleSwitchShouldBeOn()
        {
            //if there isnt a value, then set a default value to 1
            if (!LocalSettingsServices.ShowActiveLesson.ContainsKey())
            {
                LocalSettingsServices.ShowActiveLesson.AddKey(true);
            }
            HighLightActualLessonToogleSwitch.IsOn = int.Parse(LocalSettingsServices.ShowActiveLesson.GetKeyValue()) == 1;

        }

        public static bool IsShowActiveLessonsToogleSwitchOn()
        {
            return _gui?.HighLightActualLessonToogleSwitch.IsOn ??
                LocalSettingsServices.ShowActiveLesson.ContainsKey() &&
                int.Parse(LocalSettingsServices.ShowActiveLesson.GetKeyValue()) == 1;
        }

        private void CheckIfShowTimetableAtStartupToogleSwitchShouldBeOn()
        {
            //sets default values
            if (!LocalSettingsServices.ShowTimetableAtStartup.ContainsKey())
            {
                LocalSettingsServices.ShowTimetableAtStartup.AddKey(true);
            }
            if (!LocalSettingsServices.ShowTimetableAtStartupValue.ContainsKey())
            {
                LocalSettingsServices.ShowTimetableAtStartupValue.AddKey("");
            }

            ShowTimeTableAtStartupToogleSwitch.IsOn = int.Parse(LocalSettingsServices.ShowTimetableAtStartup.GetKeyValue()) == 1;

            //fill a ComboBox.Items with names of timetables
            foreach (var t in Timetable.GetAllTimeTables(MainPage.TimeTable))
            {
                ShowTimeTableAtStartupComboBox.Items.Add(t.name);
            }

            //If switch is setted on, then we have to
            //set as selected item in ComboBox a selected
            //timetable
            if (ShowTimeTableAtStartupToogleSwitch.IsOn)
            {
                ShowTimeTableAtStartupComboBox.Visibility = Visibility.Visible;

                var nameOfSelectedItemInComboBox = LocalSettingsServices.ShowTimetableAtStartupValue.GetKeyValue();

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

            if (LocalSettingsServices.ShowTimetableAtStartup.ContainsKey())
            {
                LocalSettingsServices.ShowTimetableAtStartup.RemoveKey();
            }

            LocalSettingsServices.ShowTimetableAtStartup.AddKey(ShowTimeTableAtStartupToogleSwitch.IsOn);

            var selectedTimeTable =
                Timetable.GetAllTimeTables(MainPage.TimeTable)
                    .Find(p => p.name == (ShowTimeTableAtStartupComboBox.SelectedItem as string));

            LocalSettingsServices.ShowTimetableAtStartupValue.RemoveKey();
            LocalSettingsServices.ShowTimetableAtStartupValue.AddKey(selectedTimeTable.name);
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
