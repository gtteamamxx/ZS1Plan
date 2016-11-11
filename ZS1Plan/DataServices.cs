﻿using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;

namespace ZS1Plan
{
    public static class DataServices
    {
        private static readonly StorageFolder LocalFolder = ApplicationData.Current.LocalFolder;
        private static readonly string planFileName = "ZS1Plan.xml";

        public static bool IsFileExists() => File.Exists(Path.Combine(LocalFolder.Path, planFileName));

        public static async Task<SchoolTimetable> Deserialize()
        {
            var xmlSerializer = new XmlSerializer(typeof(SchoolTimetable));

            if (!File.Exists(Path.Combine(LocalFolder.Path, planFileName)))
            {
                return null;
            }

            var fileToRead = await LocalFolder.GetFileAsync(planFileName);

            using (var textReader = new StringReader(await FileIO.ReadTextAsync(fileToRead)))
            {
                return (SchoolTimetable)xmlSerializer.Deserialize(textReader);
            }
        }

        public static async Task<bool> Serialize(SchoolTimetable toSerialize)
        {
            var xmlSerializer = new XmlSerializer(typeof(SchoolTimetable));

            using (var textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);

                StorageFile fileToSave;

                if (!File.Exists(Path.Combine(LocalFolder.Path, planFileName)))
                {
                    fileToSave = await LocalFolder.CreateFileAsync(planFileName);
                }
                else
                {
                    fileToSave = await LocalFolder.GetFileAsync(planFileName);
                }

                if (fileToSave == null)
                {
                    return false;
                }

                await FileIO.WriteTextAsync(fileToSave, textWriter.ToString());

                return true;
            }
        }
    }
}