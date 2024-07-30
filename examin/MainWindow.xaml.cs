using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace examin
{
    public partial class MainWindow : Window
    {
        private static readonly string configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "examin");
        private static readonly string configFile = Path.Combine(configDirectory, "config.json");
        private Config config;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(configFile))
            {
                var configType = typeof(Config);

                foreach (var field in configType.GetProperties())
                {
                    var label = new Label
                    {
                        Content = field.Name,
                        Margin = new(0, 0, 10, 0),
                        FontSize = 20
                    };
                    Grid.SetColumn(label, 0);

                    Control inputField = field.Name.ToLower() == "password" ? new PasswordBox() : new TextBox();
                    inputField.Name = field.Name.Replace(" ", "").Replace("-", "");
                    inputField.MinWidth = 500;
                    inputField.VerticalAlignment = VerticalAlignment.Center;
                    inputField.Padding = new(2, 5, 2, 5);
                    inputField.FontSize = 20;
                    Grid.SetColumn(inputField, 1);

                    Main.Children.Add(new Grid
                    {
                        Children = { label, inputField },
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
                            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                        },
                        Margin = new(0, 0, 0, 10)
                    });
                }

                var generateConfigButton = new Button
                {
                    Content = "Generate Config",
                    FontSize = 20,
                    Padding = new(2, 5, 2, 5)
                };
                generateConfigButton.Click += OnGenerateConfig;
                Main.Children.Add(generateConfigButton);

                Main.HorizontalAlignment = HorizontalAlignment.Center;
                Main.VerticalAlignment = VerticalAlignment.Center;
                Main.Margin = new(10);
                MinWidth = 700;
                MinHeight = 350;
            }
            else
            {
                config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configFile));
            }
        }

        private void OnGenerateConfig(object sender, RoutedEventArgs e)
        {

            try
            {
                var config = Config.FromUIElementCollection(Main.Children);

                Directory.CreateDirectory(configDirectory);
                File.WriteAllText(configFile, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));

                Main.Children.Clear();
                MessageBox.Show("Config generated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public struct Config
    {
        [JsonPropertyName("Calendar ID")]
        public string CalendarID { get; set; }

        [JsonPropertyName("School-URL")]
        public string SchoolURL { get; set; }

        [JsonPropertyName("Schoolname")]
        public string Schoolname { get; set; }

        [JsonPropertyName("Username")]
        public string Username { get; set; }

        [JsonPropertyName("Password")]
        public string Password { get; set; }

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
                        switch (textBox.Name)
                        {
                            case "CalendarID": config.CalendarID = textBox.Text; break;
                            case "SchoolURL": config.SchoolURL = textBox.Text; break;
                            case "Schoolname": config.Schoolname = textBox.Text; break;
                            case "Username": config.Username = textBox.Text; break;
                            case "Password": config.Password = textBox.Text; break;
                        }
                    }
                    else if (inputField is PasswordBox passwordBox) { config.Password = passwordBox.Password; }
                }
            }

            if (config.CalendarID is null or "" || config.SchoolURL is null or "" || config.Schoolname is null or "" || config.Username is null or "" || config.Password is null or "")
            {
                throw new Exception("One or more fields are empty!");
            }

            return config;
        }
    }
}