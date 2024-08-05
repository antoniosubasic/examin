using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Controls;

namespace examin
{
    internal struct Config
    {
        [JsonPropertyName("CalendarID")]
        public string CalendarID { get; set; }

        [JsonPropertyName("SchoolURL")]
        public string SchoolURL { get; set; }

        [JsonPropertyName("Schoolname")]
        public string Schoolname { get; set; }

        [JsonPropertyName("Username")]
        public string Username { get; set; }

        [JsonPropertyName("Password")]
        public string Password { get; set; }

        [JsonIgnore]
        private string _dateFormat { get; set; }

        [JsonPropertyName("DateFormat")]
        public string DateFormat
        {
            get => string.IsNullOrEmpty(_dateFormat) ? "dd.MM.yyyy" : _dateFormat;
            set { _dateFormat = value; }
        }

        [JsonIgnore]
        public static string Directory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "examin");

        [JsonIgnore]
        public static string File => Path.Combine(Directory, "config.json");

        public static Config ReadFromFile() => JsonSerializer.Deserialize<Config>(System.IO.File.ReadAllText(File));

        public void WriteToFile()
        {
            if (!System.IO.Directory.Exists(Directory)) { System.IO.Directory.CreateDirectory(Directory); }
            System.IO.File.WriteAllText(File, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static Config FromUIElementCollection(UIElementCollection UIElementCollection)
        {
            var config = new Config();

            foreach (var element in UIElementCollection)
            {
                if (element is Grid gridElement)
                {
                    var inputField = gridElement.Children[1];

                    if (inputField is TextBox textBox)
                    {
                        var text = textBox.Text.Trim();

                        switch (textBox.Name)
                        {
                            case "CalendarID": config.CalendarID = text; break;
                            case "SchoolURL": config.SchoolURL = text; break;
                            case "Schoolname": config.Schoolname = text; break;
                            case "Username": config.Username = text; break;
                            case "Password": config.Password = text; break;
                            case "DateFormat": config.DateFormat = text; break;
                        }
                    }
                    else if (inputField is PasswordBox passwordBox) { config.Password = passwordBox.Password; }
                }
            }

            string[] requiredFields = [config.SchoolURL, config.Schoolname, config.Username, config.Password];
            if (requiredFields.Any(string.IsNullOrEmpty)) { throw new Exception("One or more required fields are empty!"); }

            return config;
        }
    }
}
