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
    public partial class MainWindow : Window
    {
        private Settings _settings;
        private School _school;
        private Session? _session;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += (sender, e) =>
            {
                if (!File.Exists(Settings.File) || !File.Exists(School.File)) { LoadSettings(); }
                else
                {
                    _school = School.ReadFromFile();
                    LoadHome();
                }
            };
        }

        // TODO: add settings button (top right)
        private void LoadHome()
        {
            var userAuthenticationStackPanel = new StackPanel();
            var examFetchingStackPanel = new StackPanel { IsEnabled = false };

            #region User Authentication
            var username = new TextBox { Margin = new(0, 0, 0, 10), HorizontalContentAlignment = HorizontalAlignment.Center };
            var password = new PasswordBox { Margin = new(0, 0, 0, 20), HorizontalContentAlignment = HorizontalAlignment.Center };
            var login = new Button { Content = "Login", HorizontalContentAlignment = HorizontalAlignment.Center };

            login.Click += async (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(username.Text) && !string.IsNullOrWhiteSpace(password.Password))
                {
                    username.IsEnabled = password.IsEnabled = login.IsEnabled = false;

                    if (_session is not null && _session.LoggedIn)
                    {
                        _session = null;
                        username.Text = password.Password = string.Empty;
                        login.Content = "Login";
                        examFetchingStackPanel.IsEnabled = false;

                        username.IsEnabled = password.IsEnabled = login.IsEnabled = true;
                    }
                    else
                    {
                        _session = new(_school, username.Text, password.Password);
                        await _session.TryLogin();

                        if (_session.LoggedIn)
                        {
                            login.Content = "Logout";
                            login.IsEnabled = examFetchingStackPanel.IsEnabled = true;
                        }
                        else
                        {
                            MessageBox.Show("Error logging in. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            username.IsEnabled = password.IsEnabled = login.IsEnabled = true;
                        }
                    }
                }
            };

            userAuthenticationStackPanel.Children.Add(username);
            userAuthenticationStackPanel.Children.Add(password);
            userAuthenticationStackPanel.Children.Add(login);

            userAuthenticationStackPanel.Margin = new(0, 0, 25, 0);
            userAuthenticationStackPanel.MinWidth = 350;

            Grid.SetColumn(userAuthenticationStackPanel, 0);
            Grid.SetRow(userAuthenticationStackPanel, 1);
            #endregion

            #region Exam Fetching
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

            examFetchingStackPanel.Children.Add(dateTimeFrom);
            examFetchingStackPanel.Children.Add(dateTimeTo);
            examFetchingStackPanel.Children.Add(fetchExams);

            examFetchingStackPanel.Margin = new(25, 0, 0, 0);
            examFetchingStackPanel.MinWidth = 200;

            Grid.SetColumn(examFetchingStackPanel, 1);
            Grid.SetRow(examFetchingStackPanel, 1);
            #endregion

            var schoolName = new Label { Content = _school.Name, FontWeight = FontWeights.Bold, Margin = new(0, 0, 0, 35), HorizontalContentAlignment = HorizontalAlignment.Center };
            Grid.SetRow(schoolName, 0);
            Grid.SetColumnSpan(schoolName, 2);

            Main.Content = new Grid()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { schoolName, userAuthenticationStackPanel, examFetchingStackPanel },
                RowDefinitions =
                {
                    new() { Height = new GridLength(1, GridUnitType.Auto) },
                    new() { Height = new GridLength(1, GridUnitType.Star) }
                },
                ColumnDefinitions =
                {
                    new() { Width = new GridLength(1, GridUnitType.Star) },
                    new() { Width = new GridLength(1, GridUnitType.Star) }
                },
                Margin = new(10)
            };
        }

        private void LoadSettings()
        {
            var changesGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new(3, GridUnitType.Star) },
                    new ColumnDefinition { Width = new(2, GridUnitType.Star) }
                },
                Margin = new(0, 0, 0, 10)
            };
            var aliasesStackPanel = new StackPanel();

            #region Changes
            var changeSchoolStackPanel = new StackPanel { Margin = new(0, 0, 50, 0) };
            Grid.SetColumn(changeSchoolStackPanel, 0);

            var changeFormatsStackPanel = new StackPanel { Margin = new(50, 0, 0, 0) };
            Grid.SetColumn(changeFormatsStackPanel, 1);

            #region School
            var searchSchoolQuery = new TextBox { Margin = new(0, 0, 10, 10) };
            Grid.SetRow(searchSchoolQuery, 0);
            Grid.SetColumn(searchSchoolQuery, 0);

            var searchSchoolButton = new Button { Content = "Search School", Margin = new(0, 0, 0, 10) };
            Grid.SetRow(searchSchoolButton, 0);
            Grid.SetColumn(searchSchoolButton, 1);

            var schoolsCombobox = new ComboBox { DisplayMemberPath = "Name", IsEnabled = File.Exists(School.File), Margin = new(0, 0, 10, 0) };
            if (File.Exists(School.File))
            {
                schoolsCombobox.ItemsSource = new[] { _school };
                schoolsCombobox.SelectedIndex = 0;
            }
            Grid.SetRow(schoolsCombobox, 1);
            Grid.SetColumn(schoolsCombobox, 0);

            var selectSchoolButton = new Button { Content = "Select School", IsEnabled = File.Exists(School.File) };
            Grid.SetRow(selectSchoolButton, 1);
            Grid.SetColumn(selectSchoolButton, 1);

            async Task SearchSchool()
            {
                searchSchoolQuery.IsEnabled = searchSchoolButton.IsEnabled = false;

                try
                {
                    var schools = await Session.SearchSchool(searchSchoolQuery.Text);

                    if (schools.Any())
                    {
                        selectSchoolButton.IsEnabled = schoolsCombobox.IsEnabled = true;
                        schoolsCombobox.ItemsSource = schools;
                        schoolsCombobox.SelectedIndex = 0;
                    }
                    else { MessageBox.Show("No schools found!", "Info", MessageBoxButton.OK, MessageBoxImage.Information); }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }

                searchSchoolQuery.IsEnabled = searchSchoolButton.IsEnabled = true;
            }

            searchSchoolButton.Click += async (sender, e) => await SearchSchool();
            searchSchoolButton.KeyDown += async (sender, e) => { if (e.Key == Key.Enter) { await SearchSchool(); } };

            selectSchoolButton.Click += (sender, e) =>
            {
                searchSchoolQuery.Text = string.Empty;
                ((School)schoolsCombobox.SelectedItem).WriteToFile();
            };

            changeSchoolStackPanel.Children.Add(new Label { Content = "Select School", Margin = new(0, 0, 0, 10) });
            changeSchoolStackPanel.Children.Add(new Grid
            {
                Children = { searchSchoolQuery, searchSchoolButton, schoolsCombobox, selectSchoolButton },
                RowDefinitions =
                {
                    new RowDefinition { Height = new(1, GridUnitType.Star) },
                    new RowDefinition { Height = new(1, GridUnitType.Star) }
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new(3, GridUnitType.Star) },
                    new ColumnDefinition { Width = new(1, GridUnitType.Star) }
                }
            });
            #endregion

            #region Formats
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

                changeFormatsStackPanel.Children.Add(new Grid
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Children = { fieldName, fieldInput },
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = new(1, GridUnitType.Auto) }
                    },
                    Margin = new(0, 0, 0, 10)
                });
            }

            var saveFormats = new Button { Content = "Save Formats" };
            saveFormats.Click += (sender, e) =>
            {
                changeFormatsStackPanel.IsEnabled = false;

                _settings = Settings.FromUIElementCollection(changeFormatsStackPanel.Children);
                _settings.WriteToFile();

                var properties = typeof(Settings).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => prop.GetCustomAttribute<JsonIgnoreAttribute>() == null);
                foreach (var uiElement in changeFormatsStackPanel.Children.Cast<UIElement>())
                {
                    if (uiElement is Grid grid)
                    {
                        var fieldInput = (TextBox)grid.Children[1];
                        var property = properties.First(prop => prop.Name == fieldInput.Name);
                        fieldInput.Text = (string?)property.GetValue(_settings);
                    }
                }

                changeFormatsStackPanel.IsEnabled = true;
            };

            changeFormatsStackPanel.Children.Add(saveFormats);
            #endregion

            changesGrid.Children.Add(changeSchoolStackPanel);
            changesGrid.Children.Add(changeFormatsStackPanel);
            #endregion

            #region Aliases
            aliasesStackPanel.Children.Add(new Label { Content = "Aliases" });
            #endregion

            Main.Content = new StackPanel
            {
                Children = { changesGrid, aliasesStackPanel },
                Margin = new(50)
            };
        }

        // TODO: add back button (top left)
        private void LoadExams(IEnumerable<Exam> exams)
        {
            var storeExamsGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new(1, GridUnitType.Star)},
                    new ColumnDefinition { Width = new(1, GridUnitType.Star)}
                },
                Margin = new(0, 0, 0, 20)
            };
            var examsStackPanel = new StackPanel { MinWidth = 400 };

            #region Store Exams
            var saveExams = new Button { Content = "Save to File", Padding = new(12.5, 7.5, 12.5, 7.5), Margin = new(0, 0, 10, 0), HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(saveExams, 0);
            saveExams.Click += (sender, e) =>
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

                    for (var i = 0; i < examsStackPanel.Children.Count; i++)
                    {
                        var grid = (Grid)((Border)examsStackPanel.Children[i]).Child;
                        var isChecked = ((ToggleButton)grid.Children[2]).IsChecked;

                        if (isChecked!.Value)
                        {
                            var exam = exams.ElementAt(i);
                            writer.WriteLine($"{exam.Subject},{exam.Description},{exam.Date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)},{exam.StartTime:h:mm tt},{exam.EndTime:h:mm tt}");
                        }
                    }
                }
            };

            var pushExams = new Button { Content = "Push to Google Calendar", Padding = new(12.5, 7.5, 12.5, 7.5), HorizontalAlignment = HorizontalAlignment.Left };
            Grid.SetColumn(pushExams, 1);
            pushExams.Click += (sender, e) =>
            {
                MessageBox.Show("Push to Google Calendar");
            };

            storeExamsGrid.Children.Add(saveExams);
            storeExamsGrid.Children.Add(pushExams);
            #endregion

            #region Exams
            for (var i = 0; i < exams.Count(); i++)
            {
                var exam = exams.ElementAt(i);
                exam.TranslateSubject();

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

                examsStackPanel.Children.Add(new Border
                {
                    Child = grid,
                    Margin = new(0, 10, 0, 10),
                    Padding = new(25, 17.5, 25, 17.5),
                    Background = i % 2 == 0 ? Brushes.White : Brushes.LightGray,
                    CornerRadius = new(5)
                });
            }
            #endregion

            Main.Content = new StackPanel
            {
                Children = { storeExamsGrid, examsStackPanel },
                Margin = new(10, 20, 10, 10)
            };
        }
    }
}