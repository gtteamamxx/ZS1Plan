using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;

namespace ZS1Plan
{
    public abstract class MemoryInfo
    {
        protected virtual bool ContainsKey(string key) => ApplicationData.Current.LocalSettings.Values.ContainsKey(key);
        protected virtual void RemoveKey(string key) => ApplicationData.Current.LocalSettings.Values.Remove(key);
        protected virtual string GetKeyValue(string key) => ApplicationData.Current.LocalSettings.Values[key] as string;
        public virtual void AddKey(params object[] obj) => ApplicationData.Current.LocalSettings.Values.Add((string)obj[0], (string)obj[1]);
    }

    public sealed class LocalSettingsServices
    {
        public static AppThemeInfo AppTheme;
        public static ShowActiveLessonInfo ShowActiveLesson;
        public static ShowTimetableAtStartupInfo ShowTimetableAtStartup;
        public static ShowTimetableAtStartupValueInfo ShowTimetableAtStartupValue;

        public LocalSettingsServices()
        {
            AppTheme = new AppThemeInfo();
            ShowActiveLesson = new ShowActiveLessonInfo();
            ShowTimetableAtStartup = new ShowTimetableAtStartupInfo();
            ShowTimetableAtStartupValue = new ShowTimetableAtStartupValueInfo();
        }
    }

    public sealed class ShowTimetableAtStartupValueInfo : MemoryInfo
    {
        public string GetKeyValue() => base.GetKeyValue("ShowTimetableAtStartupSelectedPlan");
        public bool ContainsKey() => base.ContainsKey("ShowTimetableAtStartupSelectedPlan");
        public void RemoveKey() => base.RemoveKey("ShowTimetableAtStartupSelectedPlan");
        public override void AddKey(params object[] value) => base.AddKey("ShowTimetableAtStartupSelectedPlan", value[0]);
    }

    public sealed class ShowTimetableAtStartupInfo : MemoryInfo
    {
        public string GetKeyValue() => base.GetKeyValue("ShowTimetableAtStartup");
        public bool ContainsKey() => base.ContainsKey("ShowTimetableAtStartup");
        public void RemoveKey() => base.RemoveKey("ShowTimetableAtStartup");
        public override void AddKey(params object[] value) => base.AddKey("ShowTimetableAtStartup", (bool)value[0] ? "1" : "0");
    }

    public sealed class ShowActiveLessonInfo : MemoryInfo
    {
        public string GetKeyValue() => base.GetKeyValue("ShowActiveLessons");
        public bool ContainsKey() => base.ContainsKey("ShowActiveLessons");
        public void RemoveKey() => base.RemoveKey("ShowActiveLessons");
        public override void AddKey(params object[] value) => base.AddKey("ShowActiveLessons", (bool)value[0] ? "1" : "0");
    }

    public sealed class AppThemeInfo : MemoryInfo
    {
        public string GetKeyValue() => base.GetKeyValue("AppTheme");
        public bool ContainsKey() => base.ContainsKey("AppTheme");
        public void RemoveKey() => base.RemoveKey("AppTheme");
        public override void AddKey(params object[] theme) => base.AddKey("AppTheme",
                    (ApplicationTheme)theme[0] == ApplicationTheme.Dark ? ((int)ApplicationTheme.Light).ToString() : ((int)ApplicationTheme.Dark).ToString());
    }
}
