using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace ZS1Plan
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();

            NightModeToogleSwitch.IsOn = ApplicationData.Current.LocalSettings.Values.ContainsKey("AppTheme")
                 && ((int.Parse((ApplicationData.Current.LocalSettings.Values["AppTheme"] as string))) == 1);

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

            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage.SetTitleText("Ustawienia");

        }
    }
}
