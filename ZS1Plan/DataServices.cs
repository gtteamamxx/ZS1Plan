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

            if (!IsFileExists())
            {
                return null;
            }

            try
            {
                var fileToRead = await LocalFolder.GetFileAsync(planFileName);

                using (var textReader = new StringReader(await FileIO.ReadTextAsync(fileToRead)))
                {
                    return (SchoolTimetable) xmlSerializer.Deserialize(textReader);
                }
            }
            catch
            {
                return null;
            }
        }

        public static async Task<bool> Serialize(SchoolTimetable toSerialize)
        {
            var xmlSerializer = new XmlSerializer(typeof(SchoolTimetable));

            using (var textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);

                StorageFile fileToSave;

                if (!IsFileExists())
                {
                    try
                    {
                        fileToSave = await LocalFolder.CreateFileAsync(planFileName);
                    }
                    catch
                    {
                        fileToSave = null;
                    }
                }
                else
                {
                    try
                    {
                        fileToSave = await LocalFolder.GetFileAsync(planFileName);
                    }
                    catch
                    {
                        fileToSave = null;
                    }
                }

                if (fileToSave == null)
                {
                    return false;
                }

                try
                {
                    await FileIO.WriteTextAsync(fileToSave, textWriter.ToString());
                }
                catch
                {
                    return false;
                }

                return true;
            }
        }
    }
}
