using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using System.Globalization;

namespace examin
{
    public partial class MainWindow : Window
    {
        private static readonly string _configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "examin");
        private static readonly string _configFile = Path.Combine(_configDirectory, "config.json");
        private Config _config;
        private string _dateTimeFormat = "dd.MM.yyyy";

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(_configFile))
            {
                var configType = typeof(Config);

                foreach (var field in configType.GetProperties())
                {
                    var label = new Label { Content = field.Name };
                    Grid.SetColumn(label, 0);

                    Control inputField = field.Name.ToLower() == "password" ? new PasswordBox() : new TextBox();
                    inputField.Name = field.Name.Replace(" ", "").Replace("-", "");
                    inputField.MinWidth = 500;
                    Grid.SetColumn(inputField, 1);

                    Main.Children.Add(new Grid
                    {
                        Children = { label, inputField },
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
                            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                        }
                    });
                }

                var generateConfigButton = new Button { Content = "Generate Config" };
                generateConfigButton.Click += OnGenerateConfig;
                Main.Children.Add(generateConfigButton);

                MinWidth = 700;
                MinHeight = 350;
            }
            else
            {
                _config = JsonSerializer.Deserialize<Config>(File.ReadAllText(_configFile));

                var dateTimeFrom = new TextBox
                {
                    Text = new DateOnly(DateTime.Now.Year - (DateTime.Now.Month <= 7 ? 1 : 0), 9, 1).ToString(_dateTimeFormat),
                    MinWidth = 200,
                    TextAlignment = TextAlignment.Center
                };

                var dateTimeTo = new TextBox
                {
                    Text = new DateOnly(DateTime.Now.Year + (DateTime.Now.Month <= 7 ? 0 : 1), 7, 8).ToString(_dateTimeFormat),
                    MinWidth = 200,
                    TextAlignment = TextAlignment.Center
                };

                var fetchExams = new Button { Content = "Fetch Exams" };
                fetchExams.Click += async (sender, e) => await OnFetchExams(dateTimeFrom, dateTimeTo, fetchExams);

                Main.Children.Add(dateTimeFrom);
                Main.Children.Add(dateTimeTo);
                Main.Children.Add(fetchExams);
            }
        }

        private void OnGenerateConfig(object sender, RoutedEventArgs e)
        {

            try
            {
                var config = Config.FromUIElementCollection(Main.Children);

                Directory.CreateDirectory(_configDirectory);
                File.WriteAllText(_configFile, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));

                Main.Children.Clear();
                MessageBox.Show("Config generated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task OnFetchExams(TextBox dateTimeFrom, TextBox dateTimeTo, Button fetchExams)
        {
            foreach (var element in new UIElement[] { dateTimeFrom, dateTimeTo, fetchExams }) { element.IsEnabled = false; }
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                var from = DateOnly.ParseExact(dateTimeFrom.Text, _dateTimeFormat, CultureInfo.InvariantCulture);
                var to = DateOnly.ParseExact(dateTimeTo.Text, _dateTimeFormat, CultureInfo.InvariantCulture);

                var session = new WebuntisAPI.Session(_config);
                await session.TryLogin();

                var exams = await session.TryGetExams(from, to);
                foreach (var exam in exams) { exam.TranslateSubject(); }
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid date format!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                foreach (var element in new UIElement[] { dateTimeFrom, dateTimeTo, fetchExams }) { element.IsEnabled = true; }
                Mouse.OverrideCursor = null;
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