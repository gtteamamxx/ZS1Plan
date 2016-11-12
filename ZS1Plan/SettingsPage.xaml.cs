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

            CheckIfNightModeToogleSwitchShoudBeOn();
            CheckIfShowActiveLessonsToogleSwitchShouldBeOn();
            CheckIfShowTimetableAtStartupToogleSwitchShouldBeOn();

            NightModeToogleSwitch.Toggled += (s, e) =>
            {
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("AppTheme"))
                {
                    ApplicationData.Current.LocalSettings.Values.Remove("AppTheme");
                }

                ApplicationData.Current.LocalSettings.Values.Add("AppTheme",
                    Application.Current.RequestedTheme == ApplicationTheme.Dark ? ((int)ApplicationTheme.Light).ToString() : ((int)ApplicationTheme.Dark).ToString());

                var headerTextBlock = ((TextBlock)NightModeToogleSwitch.Header);

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

                OnHighLightActiveLessonsChanged?.Invoke();
            };

            ShowTimeTableAtStartupToogleSwitch.Toggled += (s, e) =>
            {
                if (ShowTimeTableAtStartupToogleSwitch.IsOn == false)
                {
                    if (ApplicationData.Current.LocalSettings.Values.ContainsKey("ShowTimetableAtStartup"))
                    {
                        ApplicationData.Current.LocalSettings.Values.Remove("ShowTimetableAtStartup");
                    }

                    ApplicationData.Current.LocalSettings.Values.Add("ShowTimetableAtStartup",
                        ShowTimeTableAtStartupToogleSwitch.IsOn ? "1" : "0");
                }
                else
                {
                    if (ApplicationData.Current.LocalSettings.Values.ContainsKey("ShowTimetableAtStartupSelectedPlan"))
                    {
                        if ( (string) ApplicationData.Current.LocalSettings.Values["ShowTimetableAtStartupSelectedPlan"] != "")
                        {
                            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("ShowTimetableAtStartup"))
                            {
                                ApplicationData.Current.LocalSettings.Values.Remove("ShowTimetableAtStartup");
                            }

                            ApplicationData.Current.LocalSettings.Values.Add("ShowTimetableAtStartup",
                                ShowTimeTableAtStartupToogleSwitch.IsOn ? "1" : "0");
                        }
                    }
                }
                ShowTimeTableAtStartupComboBox.Visibility = ShowTimeTableAtStartupToogleSwitch.IsOn ? Visibility.Visible : Visibility.Collapsed;
            };
        }

        private void CheckIfNightModeToogleSwitchShoudBeOn()
        {
            NightModeToogleSwitch.IsOn = ApplicationData.Current.LocalSettings.Values.ContainsKey("AppTheme")
                 && ((int.Parse((ApplicationData.Current.LocalSettings.Values["AppTheme"] as string))) == 1);
        }

        private void CheckIfShowActiveLessonsToogleSwitchShouldBeOn()
        {
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("ShowActiveLessons"))
            {
                ApplicationData.Current.LocalSettings.Values.Add("ShowActiveLessons", 1.ToString());
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

            foreach (var t in MainPage.TimeTable.GetAllTimeTables())
            {
                ShowTimeTableAtStartupComboBox.Items.Add(t.name);
            }

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
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage.SetTitleText("Ustawienia");

            if (MainPage.InfoCenterStackPanelVisibility == Visibility.Visible)
            {
                MainPage.InfoCenterStackPanelVisibility = Visibility.Collapsed;
            }
        }

        private bool _firstTimeSettingsPageOpened;
        private void ShowTimeTableAtStartupComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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
    }
}
