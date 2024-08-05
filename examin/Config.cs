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
        public Settings()
        {
            _shortDateFormat = "dd.MM.yyyy";
            _longDateFormat = "dd MMMM yyyy";
            _timeFormat = "HH:mm";
        }

        [JsonIgnore]
        private string _shortDateFormat { get; set; }

        [JsonIgnore]
        private string _longDateFormat { get; set; }

        [JsonIgnore]
        private string _timeFormat { get; set; }

        [JsonPropertyName("shortDateFormat")]
        public string ShortDateFormat
        {
            readonly get => _shortDateFormat;
            set
            {
                if (string.IsNullOrEmpty(value.Trim())) { _shortDateFormat = "dd.MM.yyyy"; }
                else
                {
                    var date = new DateOnly();

                    try
                    {
                        date.ToString(value);
                        _shortDateFormat = value;
                    }
                    catch (FormatException) { _shortDateFormat = "dd.MM.yyyy"; }
                }
            }
        }

        [JsonPropertyName("longDateFormat")]
        public string LongDateFormat
        {
            readonly get => _longDateFormat;
            set
            {
                if (string.IsNullOrEmpty(value.Trim())) { _longDateFormat = "dd MMMM yyyy"; }
                else
                {
                    var date = new DateOnly();

                    try
                    {
                        date.ToString(value);
                        _longDateFormat = value;
                    }
                    catch (FormatException) { _longDateFormat = "dd MMMM yyyy"; }
                }
            }
        }

        [JsonPropertyName("timeFormat")]
        public string TimeFormat
        {
            readonly get => _timeFormat;
            set
            {
                if (string.IsNullOrEmpty(value.Trim())) { _timeFormat = "HH:mm"; }
                else
                {
                    var time = new TimeOnly();

                    try
                    {
                        time.ToString(value);
                        _timeFormat = value;
                    }
                    catch (FormatException) { _timeFormat = "HH:mm"; }
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
