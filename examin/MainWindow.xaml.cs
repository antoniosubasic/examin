using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.Win32;
using examin.Config;
using examin.WebuntisAPI;

namespace examin
{
    public enum MainWindowPages
    {
        Login,
        Home,
        Settings,
        Exams
    }

    public partial class MainWindow : Window
    {
        private Settings _settings;
        private School _school;
        private Session? _session;
        private Config.Calendar _calendar;
        private GoogleAPI.Calendar? _calendarAPI;
        private Aliases _aliases;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += (sender, e) =>
            {
                _settings = Settings.ReadFromFile();
                _aliases = Aliases.ReadFromFile();
                _calendar = Config.Calendar.ReadFromFile();
                LoadLogin();
            };

            Closing += (sender, e) => _calendarAPI?.Deauthorize();
            Closed += (sender, e) => _calendarAPI?.Deauthorize();
        }

        private void LoadLogin()
        {
            #region Select School UI
            var searchSchoolQuery = new TextBox { Margin = new(0, 0, 5, 5) };
            Grid.SetRow(searchSchoolQuery, 0);
            Grid.SetColumn(searchSchoolQuery, 0);

            var searchSchoolButton = new Button { Content = "Search School", Margin = new(5, 0, 0, 5) };
            Grid.SetRow(searchSchoolButton, 0);
            Grid.SetColumn(searchSchoolButton, 1);

            var schoolsFoundCombobox = new ComboBox { DisplayMemberPath = "Name", Margin = new(0, 5, 5, 0), IsEnabled = File.Exists(School.File) };
            Grid.SetRow(schoolsFoundCombobox, 1);
            Grid.SetColumn(schoolsFoundCombobox, 0);
            if (File.Exists(School.File))
            {
                _school = School.ReadFromFile();
                schoolsFoundCombobox.ItemsSource = new[] { _school };
                schoolsFoundCombobox.SelectedIndex = 0;
            }

            var selectSchoolButton = new Button { Content = "Select School", Margin = new(5, 5, 0, 0), IsEnabled = File.Exists(School.File) };
            Grid.SetRow(selectSchoolButton, 1);
            Grid.SetColumn(selectSchoolButton, 1);

            var selectSchool = new Grid
            {
                Children = { searchSchoolQuery, searchSchoolButton, schoolsFoundCombobox, selectSchoolButton },
                VerticalAlignment = VerticalAlignment.Center,
                RowDefinitions =
                {
                    new RowDefinition { Height = new(1, GridUnitType.Star) },
                    new RowDefinition { Height = new(1, GridUnitType.Star) }
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new(1, GridUnitType.Auto) }
                },
                MinWidth = 500,
                Margin = new(0, 0, 50, 0)
            };
            Grid.SetColumn(selectSchool, 0);
            #endregion

            #region Login UI
            var usernameInputField = new TextBox { Text = _session?.Username, Margin = new(0, 0, 0, 5), HorizontalContentAlignment = HorizontalAlignment.Center, IsEnabled = _session is null || !_session.LoggedIn };
            var passwordInputField = new PasswordBox { Password = _session?.Password, Margin = new(0, 5, 0, 10), HorizontalContentAlignment = HorizontalAlignment.Center, IsEnabled = _session is null || !_session.LoggedIn };
            var loginButton = new Button { Margin = new(0, 10, 0, 0), Content = (_session is not null && _session.LoggedIn) ? "Logout" : "Login", HorizontalContentAlignment = HorizontalAlignment.Center, IsEnabled = File.Exists(School.File) };

            var login = new StackPanel
            {
                Children = { usernameInputField, passwordInputField, loginButton },
                MinWidth = 450,
                Margin = new(0, 50, 0, 0)
            };
            Grid.SetColumn(login, 1);
            #endregion

            #region Select School Handlers
            async Task SearchSchool()
            {
                searchSchoolQuery.IsEnabled = searchSchoolButton.IsEnabled = false;
                Mouse.OverrideCursor = Cursors.Wait;

                try
                {
                    var schools = await Session.SearchSchool(searchSchoolQuery.Text);

                    if (schools.Any())
                    {
                        schoolsFoundCombobox.ItemsSource = schools;
                        schoolsFoundCombobox.SelectedIndex = 0;
                        selectSchoolButton.IsEnabled = schoolsFoundCombobox.IsEnabled = true;
                    }
                    else { MessageBox.Show("No schools found!", "Info", MessageBoxButton.OK, MessageBoxImage.Information); }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }

                searchSchoolQuery.IsEnabled = searchSchoolButton.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }

            searchSchoolQuery.KeyDown += async (sender, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    await SearchSchool();
                    searchSchoolQuery.Focus();
                }
            };
            searchSchoolButton.Click += async (sender, e) => await SearchSchool();

            selectSchoolButton.Click += (sender, e) =>
            {
                if (_session is not null)
                {
                    _session = null;
                    LoadLogin();
                }

                usernameInputField.IsEnabled = passwordInputField.IsEnabled = loginButton.IsEnabled = true;
                searchSchoolQuery.Text = "";
                (_school = (School)schoolsFoundCombobox.SelectedItem).WriteToFile();
            };
            #endregion

            #region Login Handlers
            async Task Login()
            {
                if (!string.IsNullOrEmpty(usernameInputField.Text) && !string.IsNullOrEmpty(passwordInputField.Password))
                {
                    usernameInputField.IsEnabled = passwordInputField.IsEnabled = loginButton.IsEnabled = false;
                    Mouse.OverrideCursor = Cursors.Wait;

                    if (_session is not null && _session.LoggedIn)
                    {
                        _session = null;
                        LoadLogin();
                    }
                    else
                    {
                        _session = new(_school, usernameInputField.Text, passwordInputField.Password);
                        await _session.TryLogin();

                        if (_session.LoggedIn)
                        {
                            loginButton.Content = "Logout";
                            loginButton.IsEnabled = true;
                            Mouse.OverrideCursor = null;
                            LoadHome();
                        }
                        else
                        {
                            MessageBox.Show("Error logging in. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            usernameInputField.IsEnabled = passwordInputField.IsEnabled = loginButton.IsEnabled = true;
                        }
                    }

                    Mouse.OverrideCursor = null;
                }
            }

            passwordInputField.KeyDown += async (sender, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    await Login();
                    loginButton.Focus();
                }
            };
            loginButton.Click += async (sender, e) => await Login();
            #endregion

            #region Menu UI + Handlers
            var navigateToHome = new MenuItem { Header = "Navigate to Home" };
            navigateToHome.Click += (sender, e) => LoadHome();

            var navigateToSettings = new MenuItem { Header = "Navigate to Settings" };
            navigateToSettings.Click += (sender, e) => LoadSettings(MainWindowPages.Login);

            var menu = new Menu();
            if (_session is not null && _session.LoggedIn) { menu.Items.Add(navigateToHome); }
            menu.Items.Add(navigateToSettings);
            #endregion

            Main.Content = new DockPanel
            {
                Children =
                {
                    menu,
                    new Grid
                    {
                        Children = { selectSchool, login },
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = new(1, GridUnitType.Star) },
                            new ColumnDefinition { Width = new(1, GridUnitType.Star) }
                        },
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new(10)
                    }
                }
            };
        }

        private void LoadHome()
        {
            // TODO: add automatic schoolyear detection from API
            #region Exam Fetching UI
            var dateTimeFrom = new TextBox
            {
                Text = new DateOnly(DateTime.Now.Year - (DateTime.Now.Month <= 8 ? 1 : 0), 9, 1).ToString(_settings.ShortDateFormat),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = new(0, 0, 0, 10)
            };

            var dateTimeTo = new TextBox
            {
                Text = new DateOnly(DateTime.Now.Year + (DateTime.Now.Month <= 8 ? 0 : 1), 7, 8).ToString(_settings.ShortDateFormat),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = new(0, 0, 0, 20)
            };

            var fetchExams = new Button { Content = "Fetch Exams", HorizontalContentAlignment = HorizontalAlignment.Center };

            var examFetching = new StackPanel
            {
                Children =
                {
                    new Label { Content = _school.Name, FontWeight = FontWeights.Bold, Margin = new(0, 0, 0, 35), HorizontalContentAlignment = HorizontalAlignment.Center },
                    dateTimeFrom,
                    dateTimeTo,
                    fetchExams
                },
                MinWidth = 200,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new(10)
            };
            #endregion

            #region Exam Fetching Handlers
            fetchExams.Click += async (sender, e) =>
            {
                dateTimeFrom.IsEnabled = dateTimeTo.IsEnabled = fetchExams.IsEnabled = false;
                Mouse.OverrideCursor = Cursors.Wait;

                var from = DateOnly.ParseExact(dateTimeFrom.Text, _settings.ShortDateFormat, CultureInfo.InvariantCulture);
                var to = DateOnly.ParseExact(dateTimeTo.Text, _settings.ShortDateFormat, CultureInfo.InvariantCulture);

                var exams = await _session!.TryGetExams(from, to);

                if (exams.Any()) { LoadExams(exams); }
                else { MessageBox.Show("No exams found in the given time frame.", "No Exams", MessageBoxButton.OK, MessageBoxImage.Information); }

                dateTimeFrom.IsEnabled = dateTimeTo.IsEnabled = fetchExams.IsEnabled = true;
                Mouse.OverrideCursor = null;
            };
            #endregion

            #region Menu UI + Handlers
            var backToLogin = new MenuItem { Header = "Back to Login" };
            backToLogin.Click += (sender, e) => LoadLogin();

            var navigateToSettings = new MenuItem { Header = "Navigate to Settings" };
            navigateToSettings.Click += (sender, e) => LoadSettings(MainWindowPages.Home);

            var menu = new Menu { Items = { backToLogin, navigateToSettings } };
            #endregion

            Main.Content = new DockPanel { Children = { menu, examFetching } };
        }

        private void LoadSettings(MainWindowPages previousPage)
        {
            #region Edit Formats UI
            var editFormats = new StackPanel
            {
                Children =
                {
                    new Label { Content = "Formats", HorizontalAlignment = HorizontalAlignment.Center, FontWeight = FontWeights.Bold, Margin = new(0, 0, 0, 35) }
                },
                Margin = new(0, 25, 0, 50)
            };
            Grid.SetRow(editFormats, 0);
            Grid.SetColumn(editFormats, 0);

            foreach (var property in typeof(Settings).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => prop.GetCustomAttribute<JsonIgnoreAttribute>() == null))
            {
                var fieldName = new Label
                {
                    Content = property.Name switch
                    {
                        "ShortDateFormat" => "Short Date Format",
                        "LongDateFormat" => "Long Date Format",
                        "TimeFormat" => "Time Format",
                        _ => property.Name
                    }
                };
                Grid.SetColumn(fieldName, 0);

                var fieldInput = new TextBox
                {
                    Text = (string?)property.GetValue(_settings),
                    Name = property.Name,
                    Margin = new(15, 0, 0, 0),
                    HorizontalContentAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(fieldInput, 1);

                editFormats.Children.Add(new Grid
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Children = { fieldName, fieldInput },
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = new(1, GridUnitType.Auto) }
                    },
                    Margin = new(0, 0, 25, 10)
                });
            }
            #endregion

            #region Google Calendar API UI + Handlers
            var titleLabel = new Label { Content = "Google Calendar API", HorizontalAlignment = HorizontalAlignment.Center, FontWeight = FontWeights.Bold, Margin = new(0, 0, 0, 35) };

            var clientSecretFileLabel = new Label
            {
                Content = "Client Secret",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new(0, 0, 10, 10)
            };
            Grid.SetColumn(clientSecretFileLabel, 0);

            var selectClientSecretFile = new Button { Content = "Select File", Margin = new(10, 0, 0, 10) };
            Grid.SetColumn(selectClientSecretFile, 1);
            selectClientSecretFile.Click += (sender, e) =>
            {
                var openFileDialog = new OpenFileDialog
                {
                    DefaultExt = "json",
                    AddExtension = true,
                    Filter = "JSON Files (*.json)|*.json"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    File.Copy(openFileDialog.FileName, Config.Calendar.ClientSecretFile, true);
                }
            };

            var calendarIDLabel = new Label
            {
                Content = "Calendar ID",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new(0, 0, 10, 10)
            };
            Grid.SetColumn(calendarIDLabel, 0);

            var enterCalendarID = new TextBox { Text = _calendar.CalendarID, Margin = new(10, 0, 0, 10) };
            Grid.SetColumn(enterCalendarID, 1);

            var googleCalendarAPI = new StackPanel
            {
                Children =
                {
                    titleLabel,
                    new Grid
                    {
                        Children = { clientSecretFileLabel, selectClientSecretFile },
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = new(1, GridUnitType.Auto) },
                            new ColumnDefinition { Width = new(1, GridUnitType.Star) }
                        }
                    },
                    new Grid
                    {
                        Children = { calendarIDLabel, enterCalendarID },
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = new(1, GridUnitType.Auto) },
                            new ColumnDefinition { Width = new(1, GridUnitType.Star) }
                        }
                    }
                },
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new(25, 0, 0, 0)
            };
            Grid.SetRow(googleCalendarAPI, 0);
            Grid.SetColumn(googleCalendarAPI, 1);
            #endregion

            #region Edit Aliases UI + Handlers
            var editAliases = new StackPanel
            {
                Children =
                {
                    new Label { Content = "Aliases", HorizontalAlignment = HorizontalAlignment.Center, FontWeight = FontWeights.Bold, Margin = new(0, 0, 0, 35) }
                },
                Margin = new(50, 0, 50, 0)
            };
            Grid.SetRow(editAliases, 1);
            Grid.SetColumnSpan(editAliases, 2);

            static Grid InputGrid(KeyValuePair<string, string>? alias = null)
            {
                var left = new TextBox { Text = alias?.Key, Margin = new(0, 0, 10, 0), HorizontalContentAlignment = HorizontalAlignment.Right, IsReadOnly = alias is not null };
                var right = new TextBox { Text = alias?.Value, HorizontalContentAlignment = HorizontalAlignment.Left };
                Grid.SetColumn(right, 1);

                return new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = new(1, GridUnitType.Star) }
                    },
                    Children = { left, right },
                    Margin = new(0, 0, 0, 10)
                };
            }

            var addButton = new Button { Content = "Add", Margin = new(0, 0, 0, 20), MaxWidth = 200 };
            addButton.Click += (sender, e) => editAliases.Children.Insert(2, InputGrid());
            editAliases.Children.Add(addButton);

            foreach (var alias in _aliases)
            {
                editAliases.Children.Add(InputGrid(alias));
            }
            #endregion

            #region Menu UI + Handlers
            var back = new MenuItem { Header = "Save & Back" };
            back.Click += (sender, e) =>
            {
                editFormats.IsEnabled = editAliases.IsEnabled = false;
                Mouse.OverrideCursor = Cursors.Wait;

                _settings = Settings.FromUIElementCollection(editFormats.Children);
                _settings.WriteToFile();

                _aliases.FromUIElementCollection(editAliases.Children);
                _aliases.WriteToFile();

                _calendar = new() { CalendarID = enterCalendarID.Text };
                _calendar.WriteToFile();

                editFormats.IsEnabled = editAliases.IsEnabled = true;
                Mouse.OverrideCursor = null;

                switch (previousPage)
                {
                    case MainWindowPages.Home:
                        LoadHome();
                        break;

                    case MainWindowPages.Login:
                        LoadLogin();
                        break;

                    default:
                        throw new InvalidOperationException("Invalid previous page");
                }
            };

            var menu = new Menu { Items = { back } };
            #endregion

            Main.Content = new DockPanel
            {
                Children =
                {
                    menu,
                    new Grid
                    {
                        Children = { editFormats, googleCalendarAPI, editAliases },
                        RowDefinitions =
                        {
                            new RowDefinition { Height = new(1, GridUnitType.Star) },
                            new RowDefinition { Height = new(1, GridUnitType.Star) }
                        },
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = new(1, GridUnitType.Star) },
                            new ColumnDefinition { Width = new(1, GridUnitType.Star) }
                        },
                        Margin = new(25)
                    }
                }
            };
        }

        private void LoadExams(IEnumerable<Exam> exams)
        {
            #region Exams UI
            var examsElement = new StackPanel { MinWidth = 400, Margin = new(10) };

            for (var i = 0; i < exams.Count(); i++)
            {
                var exam = exams.ElementAt(i);

                if (!string.IsNullOrEmpty(exam.Subject))
                {
                    if (_aliases.TryGetValue(exam.Subject, out var alias)) { exam.Subject = alias; }
                    else { _aliases.Add(exam.Subject, exam.Subject); }
                }

                var subject = new Label { Content = exam.Subject, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(subject, 0);

                var dateTime = new Grid
                {
                    RowDefinitions =
                    {
                        new RowDefinition { Height = new(1, GridUnitType.Auto) },
                        new RowDefinition { Height = new(1, GridUnitType.Auto) }
                    },
                    Margin = new(0)
                };
                var date = new Label { Content = exam.Date.ToString(_settings.LongDateFormat), FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetRow(date, 0);
                var time = new Label { Content = $"{exam.Start.ToString(_settings.TimeFormat)} - {exam.End.ToString(_settings.TimeFormat)}", FontSize = 16.5, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetRow(time, 1);
                dateTime.Children.Add(date);
                dateTime.Children.Add(time);
                Grid.SetColumn(dateTime, 1);

                var toggleExam = new ToggleButton { Width = 50, HorizontalAlignment = HorizontalAlignment.Right, IsChecked = true };
                toggleExam.Click += (sender, e) => { subject.IsEnabled = date.IsEnabled = time.IsEnabled = toggleExam.IsChecked!.Value; };
                Grid.SetColumn(toggleExam, 2);

                var grid = new Grid
                {
                    Children = { subject, dateTime, toggleExam },
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = new(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = new(1, GridUnitType.Star) }
                    }
                };

                examsElement.Children.Add(new Border
                {
                    Child = grid,
                    Margin = new(0, 10, 0, 10),
                    Padding = new(25, 17.5, 25, 17.5),
                    Background = i % 2 == 0 ? Brushes.White : Brushes.LightGray,
                    CornerRadius = new(5)
                });
            }

            _aliases.WriteToFile();
            #endregion

            #region Menu UI + Handlers
            var saveToFile = new MenuItem { Header = "Save to File" };
            saveToFile.Click += (sender, e) =>
            {
                var saveFileDialog = new SaveFileDialog
                {
                    FileName = "events.csv",
                    DefaultExt = "csv",
                    AddExtension = true,
                    Filter = "CSV Files (*.csv)|*.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using var writer = new StreamWriter(saveFileDialog.FileName);
                    writer.WriteLine("Subject,Description,Start Date,Start Time,End Time");

                    for (var i = 0; i < examsElement.Children.Count; i++)
                    {
                        var grid = (Grid)((Border)examsElement.Children[i]).Child;
                        var isChecked = ((ToggleButton)grid.Children[2]).IsChecked;

                        if (isChecked!.Value)
                        {
                            var exam = exams.ElementAt(i);
                            writer.WriteLine($"{exam.Subject},{exam.Description},{exam.Date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)},{exam.StartTime:h:mm tt},{exam.EndTime:h:mm tt}");
                        }
                    }
                }
            };

            var pushToGoogleCalendar = new MenuItem { Header = "Push to Google Calendar" };
            pushToGoogleCalendar.Click += (sender, e) =>
            {
                if (!File.Exists(Config.Calendar.CalendarIDFile) || !File.Exists(Config.Calendar.ClientSecretFile))
                {
                    MessageBox.Show("Please set up the Google Calendar API in the Settings", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    _calendarAPI ??= new(_calendar);

                    Main.IsEnabled = false;
                    Mouse.OverrideCursor = Cursors.Wait;
                    _calendarAPI.Authorize();
                    Main.IsEnabled = true;
                    Mouse.OverrideCursor = Cursors.Wait;

                    for (var i = 0; i < examsElement.Children.Count; i++)
                    {
                        var grid = (Grid)((Border)examsElement.Children[i]).Child;
                        var isChecked = ((ToggleButton)grid.Children[2]).IsChecked;

                        if (isChecked!.Value)
                        {
                            _calendarAPI.AddExam(exams.ElementAt(i));
                        }
                    }

                    Main.IsEnabled = false;
                    Mouse.OverrideCursor = Cursors.Wait;
                    _calendarAPI.PushExams();
                    Main.IsEnabled = true;
                    Mouse.OverrideCursor = null;
               
                    MessageBox.Show("Exams pushed to Google Calendar", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };

            var backToHome = new MenuItem { Header = "Back to Home" };
            backToHome.Click += (sender, e) => LoadHome();

            var menu = new Menu { Items = { backToHome, saveToFile, pushToGoogleCalendar } };
            #endregion

            Main.Content = new DockPanel
            {
                Children =
                {
                    menu,
                    examsElement
                }
            };
        }
    }
}