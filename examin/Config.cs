using System.Windows.Controls;
using System.IO;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;

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

        public static Settings ReadFromFile()
        {
            if (System.IO.File.Exists(File)) { return JsonSerializer.Deserialize<Settings>(System.IO.File.ReadAllText(File)); }
            else { return new(); }
        }

        public readonly void WriteToFile()
        {
            if (!Directory.Exists(Global.Directory)) { Directory.CreateDirectory(Global.Directory); }
            System.IO.File.WriteAllText(File, JsonSerializer.Serialize(this));
        }

        public static Settings FromUIElementCollection(UIElementCollection uiElementCollection)
        {
            var settings = new Settings();

            foreach (var element in uiElementCollection)
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

    internal struct Aliases(Dictionary<string, string> aliases) : IDictionary<string, string>
    {
        private Dictionary<string, string> _aliases { get; set; } = aliases ?? [];
        private Dictionary<string, string> aliases
        {
            get => _aliases ??= [];
            set => _aliases = value ?? [];
        }

        public static string File => Path.Combine(Global.Directory, "aliases.json");

        public static Aliases ReadFromFile()
        {
            if (System.IO.File.Exists(File))
            {
                var aliases = JsonSerializer.Deserialize<Dictionary<string, string>>(System.IO.File.ReadAllText(File)) ?? throw new JsonException("failed to deserialize aliases");
                return new(aliases);
            }
            else { return new([]); }
        }

        public void WriteToFile()
        {
            if (!Directory.Exists(Global.Directory)) { Directory.CreateDirectory(Global.Directory); }
            System.IO.File.WriteAllText(File, JsonSerializer.Serialize(aliases));
        }

        public void FromUIElementCollection(UIElementCollection uiElementCollection)
        {
            foreach (var element in uiElementCollection)
            {
                if (element is Grid gridElement)
                {
                    var left = ((TextBox)gridElement.Children[0]).Text.Trim();
                    var right = ((TextBox)gridElement.Children[1]).Text.Trim();

                    if (!string.IsNullOrEmpty(left) && !string.IsNullOrEmpty(right))
                    {
                        if (aliases.ContainsKey(left)) { aliases[left] = right; }
                        else { aliases.Add(left, right); }
                    }
                }
            }
        }

        public string this[string key]
        {
            get => aliases[key];
            set => aliases[key] = value;
        }

        public ICollection<string> Keys => aliases.Keys;
        public ICollection<string> Values => aliases.Values;
        public int Count => aliases.Count;
        public bool IsReadOnly => ((IDictionary<string, string>)aliases).IsReadOnly;
        public void Add(string key, string value) => aliases.Add(key, value);
        public void Add(KeyValuePair<string, string> item) => aliases.Add(item.Key, item.Value);
        public void Clear() => aliases.Clear();
        public bool Contains(KeyValuePair<string, string> item) => aliases.Contains(item);
        public bool ContainsKey(string key) => aliases.ContainsKey(key);
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            foreach (var kvp in aliases)
            {
                array[arrayIndex++] = kvp;
            }
        }
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => aliases.GetEnumerator();
        public bool Remove(KeyValuePair<string, string> item) => aliases.Remove(item.Key);
        public bool Remove(string key) => aliases.Remove(key);
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value) => aliases.TryGetValue(key, out value);
        IEnumerator IEnumerable.GetEnumerator() => aliases.GetEnumerator();
    }

    internal struct Calendar
    {
        private string _calendarID { get; set; }

        public string CalendarID
        {
            readonly get => _calendarID;
            set { _calendarID = value.EndsWith("@group.calendar.google.com") ? value[..^"@group.calendar.google.com".Length] : value; }
        }

        public static string ClientSecretFile => Path.Combine(Global.Directory, "clientSecret.json");
        public static string CalendarIDFile => Path.Combine(Global.Directory, "calendarID");

        public static Calendar ReadFromFile() => new() { CalendarID = File.Exists(CalendarIDFile) ? File.ReadAllText(CalendarIDFile) : "" };

        public readonly void WriteToFile()
        {
            if (!Directory.Exists(Global.Directory)) { Directory.CreateDirectory(Global.Directory); }
            File.WriteAllText(CalendarIDFile, CalendarID);
        }
    }
}
