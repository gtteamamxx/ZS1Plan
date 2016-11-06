using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Networking.Connectivity;
using Windows.Storage;

namespace ZS1Plan
{
    public class HTMLServices
    {
        public delegate void TimeTableDownloaded(Timetable timetable, int lenght);
        public delegate void AllTimeTablesDownloaed();

        public static event TimeTableDownloaded OnTimeTableDownloaded;
        public static event AllTimeTablesDownloaed OnAllTimeTablesDownloaded;
        public static async Task<string> getSource(string url)
        {
            var http = new HttpClient();
            var stream = await http.GetAsync(url);
            byte[] data = await stream.Content.ReadAsByteArrayAsync();

            return @Encoding.UTF8.GetString(data);
        }

        public async static Task getData()
        {
            HtmlParser parser = new HtmlParser();
            var document = await parser.ParseAsync(await @getSource("http://zs-1.pl/plan_nauczyciele/lista.html"));

            var listOfTables = document.QuerySelectorAll("ul");
            var listOfClasses = listOfTables[0];
            var listOfTeachers = listOfTables[1];

            for (int i = 0; i < listOfClasses.Children.Count(); i++)
            {
                string url = "http://zs-1.pl/plan_nauczyciele/" + listOfClasses.Children[i].FirstElementChild.GetAttribute("href");
                document = await parser.ParseAsync(await getSource(url));

                string className = document.QuerySelector("span").TextContent;

                var listOfHours = document.QuerySelectorAll("table").Where(p => p.ClassName == "tabela").ToList().First()
                    .QuerySelectorAll("tr").Where(p => p.FirstElementChild.ClassName == "nr").ToList();

                Timetable timetable = new Timetable();

                timetable.name = className;
                timetable.days = new List<Day>();

                for (int d = 0; d < 5; d++)
                {
                    Day day = new Day();
                    day.Lessons = new List<Lesson>();

                    for (int h = 0; h < listOfHours.Count(); h++)
                    {
                        Lesson lesson = new Lesson();
                        // 2, because 0 is h'our number, 1 is a ring time (eg. 7:10 -> xxx )
                        var item = listOfHours[h].Children[2 + d];

                        if (item.InnerHtml == @"&nbsp;")
                        {
                            day.Lessons.Add(lesson);
                            continue;
                        }

                        var spanList = item.QuerySelectorAll("span");
                        lesson.lesson1Name = spanList.First(p => p.ClassName == "p").TextContent;
                        lesson.lesson1Place = spanList.First(p => p.ClassName == "s").TextContent;

                        var adressList = item.QuerySelectorAll("a");

                        if (adressList.Count() == 0)
                        {
                            lesson.lesson1Tag = spanList.Where(p => p.ClassName == "p").ToList()[1].TextContent;
                            lesson.lesson1TagHref = "";
                        }
                        else
                        {
                            lesson.lesson1Tag = adressList.First(p => p.ClassName == "n").TextContent;
                            lesson.lesson1TagHref = adressList.First().GetAttribute("href");
                        }

                        if (spanList.Where(p => p.ClassName == "p").Count() == 2 && (spanList.Where(p => p.ClassName == "s").Count() == 2))
                        {
                            lesson.lesson2Name = spanList.Where(p => p.ClassName == "p").ToList()[1].TextContent;
                            lesson.lesson2Place = spanList.Where(p => p.ClassName == "s").ToList()[1].TextContent;
                            lesson.lesson2Tag = adressList.Where(p => p.ClassName == "n").ToList()[1].TextContent;
                            lesson.lesson2TagHref = adressList[1].GetAttribute("href");
                        }
                        else if (spanList.Where(p => p.ClassName == "p").Count() == 4 && (spanList.Where(p => p.ClassName == "s").Count() == 2))
                        {
                            lesson.lesson2Name = spanList.Where(p => p.ClassName == "p").ToList()[2].TextContent;
                            lesson.lesson2Place = spanList.Where(p => p.ClassName == "s").ToList()[1].TextContent;
                            lesson.lesson2Tag = spanList.Where(p => p.ClassName == "p").ToList()[3].TextContent;
                            lesson.lesson2TagHref = "";
                        }
                        day.Lessons.Add(lesson);
                    }

                    timetable.days.Add(day);
                }

                timetable.type = 0;
                OnTimeTableDownloaded?.Invoke(timetable, listOfClasses.Children.Count()+listOfTeachers.Children.Count());
            }

            for (int i = 0; i < listOfTeachers.Children.Count(); i++)
            {
                string url = "http://zs-1.pl/plan_nauczyciele/" + listOfTeachers.Children[i].FirstElementChild.GetAttribute("href");
                document = await parser.ParseAsync(await getSource(url));

                string className = document.QuerySelector("span").TextContent;

                var listOfHours = document.QuerySelectorAll("table").Where(p => p.ClassName == "tabela").ToList().First()
                    .QuerySelectorAll("tr").Where(p => p.FirstElementChild.ClassName == "nr").ToList();

                Timetable timetable = new Timetable();

                timetable.name = className;
                timetable.days = new List<Day>();

                for (int d = 0; d < 5; d++)
                {
                    Day day = new Day();
                    day.Lessons = new List<Lesson>();

                    for (int h = 0; h < listOfHours.Count(); h++)
                    {
                        Lesson lesson = new Lesson();
                        // 2, because 0 is h'our number, 1 is a ring time (eg. 7:10 -> xxx )
                        var item = listOfHours[h].Children[2 + d];

                        if (item.InnerHtml == @"&nbsp;")
                        {
                            day.Lessons.Add(lesson);
                            continue;
                        }

                        var spanList = item.QuerySelectorAll("span");
                        lesson.lesson1Name = spanList.First(p => p.ClassName == "p").TextContent;
                        lesson.lesson1Place = spanList.First(p => p.ClassName == "s").TextContent;

                        var adressList = item.QuerySelectorAll("a");
                        lesson.lesson1Tag = adressList.First(p => p.ClassName == "o").TextContent;
                        lesson.lesson1TagHref = adressList.First().GetAttribute("href");

                        lesson.lesson2Name = (item.TextContent.Contains("1/2")) ? "1/2" : item.TextContent.Contains("2/2") ? "2/2" : "";
                        day.Lessons.Add(lesson);
                    }

                    timetable.days.Add(day);
                }

                timetable.type = 1;
                OnTimeTableDownloaded?.Invoke(timetable, listOfClasses.Children.Count() + listOfTeachers.Children.Count());
            }

            OnAllTimeTablesDownloaded?.Invoke();
        }

        public static bool HasInternetConnection()
        {
            ConnectionProfile profile = NetworkInformation.GetInternetConnectionProfile();
            return (profile != null && profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess);
        }
    }

    public class DataServices
    {
        private static StorageFolder LocalFolder = ApplicationData.Current.LocalFolder;
        private static string FileName = "ZS1Plan.xml";

        public static bool IsFileExists()
        {
            if (!File.Exists(Path.Combine(LocalFolder.Path, FileName)))
                return false;
            return true;
        }
        public static async Task<SchoolTimetable> Deserialize()
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(SchoolTimetable));

            StorageFile FileToRead;

            if (!File.Exists(Path.Combine(LocalFolder.Path, FileName)))
                return null;
            else
                FileToRead = await LocalFolder.GetFileAsync(FileName);

            using (StringReader textReader = new StringReader(await FileIO.ReadTextAsync(FileToRead)))
            {
                return (SchoolTimetable)xmlSerializer.Deserialize(textReader);
            }
        }

        public static async Task Serialize(SchoolTimetable toSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(SchoolTimetable));

            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);

                StorageFile FileToSave;

                if (!File.Exists(Path.Combine(LocalFolder.Path, FileName)))
                    FileToSave = await LocalFolder.CreateFileAsync(FileName);
                else
                    FileToSave = await LocalFolder.GetFileAsync(FileName);

                await FileIO.WriteTextAsync(FileToSave, textWriter.ToString());
            }
        }
    }
}
