using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Controls;

namespace examin.Config
{
    internal static class Global
    {
        public static string Directory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "examin");
    }

    internal struct School
    {
        [JsonPropertyName("server")]
        public string ServerURL { get; set; }

        [JsonPropertyName("displayName")]
        public string Name { get; set; }

        [JsonPropertyName("loginName")]
        public string LoginName { get; set; }

        [JsonIgnore]
        public static string File => Path.Combine(Global.Directory, "school.json");

        public static School ReadFromFile() => JsonSerializer.Deserialize<School>(System.IO.File.ReadAllText(File));

        public readonly void WriteToFile()
        {
            if (!Directory.Exists(Global.Directory)) { Directory.CreateDirectory(Global.Directory); }
            System.IO.File.WriteAllText(File, JsonSerializer.Serialize(this));
        }
    }

    internal struct Settings
    {
        [JsonIgnore]
        private string _shortDateFormat { get; set; }

        [JsonIgnore]
        private string _longDateFormat { get; set; }

        [JsonIgnore]
        private string _timeFormat { get; set; }

        [JsonPropertyName("shortDateFormat")]
        public string ShortDateFormat
        {
            get
            {
                if (!string.IsNullOrEmpty(_shortDateFormat?.Trim()))
                {
                    try
                    {
                        new DateOnly().ToString(_shortDateFormat);
                        return _shortDateFormat;
                    }
                    catch (FormatException) { return _shortDateFormat = "dd.MM.yyyy"; }
                }
                else { return _shortDateFormat = "dd.MM.yyyy"; }
            }
            set
            {
                if (!string.IsNullOrEmpty(value?.Trim()))
                {
                    try
                    {
                        new DateOnly().ToString(value);
                        _shortDateFormat = value;
                    }
                    catch (FormatException) { }
                }
            }
        }

        [JsonPropertyName("longDateFormat")]
        public string LongDateFormat
        {
            get
            {
                if (!string.IsNullOrEmpty(_longDateFormat?.Trim()))
                {
                    try
                    {
                        new DateOnly().ToString(_longDateFormat);
                        return _longDateFormat;
                    }
                    catch (FormatException) { return _longDateFormat = "dd MMMM yyyy"; }
                }
                else { return _longDateFormat = "dd MMMM yyyy"; }
            }
            set
            {
                if (!string.IsNullOrEmpty(value?.Trim()))
                {
                    try
                    {
                        new DateOnly().ToString(value);
                        _longDateFormat = value;
                    }
                    catch (FormatException) { }
                }
            }
        }

        [JsonPropertyName("timeFormat")]
        public string TimeFormat
        {
            get
            {
                if (!string.IsNullOrEmpty(_timeFormat?.Trim()))
                {
                    try
                    {
                        new TimeOnly().ToString(_timeFormat);
                        return _timeFormat;
                    }
                    catch (FormatException) { return _timeFormat = "HH:mm"; }
                }
                else { return _timeFormat = "HH:mm"; }
            }
            set
            {
                if (!string.IsNullOrEmpty(value?.Trim()))
                {
                    try
                    {
                        new TimeOnly().ToString(value);
                        _timeFormat = value;
                    }
                    catch (FormatException) { }
                }
            }
        }

        [JsonIgnore]
        public static string File => Path.Combine(Global.Directory, "settings.json");

        public static Settings ReadFromFile() => JsonSerializer.Deserialize<Settings>(System.IO.File.ReadAllText(File));

        public readonly void WriteToFile()
        {
            if (!Directory.Exists(Global.Directory)) { Directory.CreateDirectory(Global.Directory); }
            System.IO.File.WriteAllText(File, JsonSerializer.Serialize(this));
        }

        public static Settings FromUIElementCollection(UIElementCollection UIElementCollection)
        {
            var settings = new Settings();

            foreach (var element in UIElementCollection)
            {
                if (element is Grid gridElement)
                {
                    var inputField = (TextBox)gridElement.Children[1];
                    var text = inputField.Text.Trim();

                    switch (inputField.Name)
                    {
                        case "ShortDateFormat": settings.ShortDateFormat = text; break;
                        case "LongDateFormat": settings.LongDateFormat = text; break;
                        case "TimeFormat": settings.TimeFormat = text; break;
                    }
                }
            }

            return settings;
        }
    }
}
