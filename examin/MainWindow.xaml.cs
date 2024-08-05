using System.Windows;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;
using examin.Config;
using examin.WebuntisAPI;
using System.Text.Json.Serialization;
using System.Reflection.Metadata;

namespace examin
{
    public partial class MainWindow : Window
    {
        private Settings _settings;
        private School _school;
        private Session? _session;
        private IEnumerable<Exam>? _exams;

        public MainWindow()
        {
            InitializeComponent();

            if (!File.Exists(Settings.File))
            {
                _settings = new();
                _settings.WriteToFile();
            }
            else { _settings = Settings.ReadFromFile(); }

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var mainStackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new(20)
            };

            if (!File.Exists(School.File))
            {
                var searchSchoolQuery = new TextBox { MinWidth = 500, Margin = new(0, 0, 5, 10) };
                Grid.SetRow(searchSchoolQuery, 0);
                Grid.SetColumn(searchSchoolQuery, 0);

                var searchSchool = new Button { Content = "Search School", Margin = new(5, 0, 0, 10) };
                Grid.SetRow(searchSchool, 0);
                Grid.SetColumn(searchSchool, 1);

                var schoolComboBox = new ComboBox { DisplayMemberPath = "Name", SelectedIndex = 0 };
                Grid.SetRow(schoolComboBox, 1);
                Grid.SetColumn(schoolComboBox, 0);

                var selectSchool = new Button { Content = "Select School", Margin = new(5, 0, 0, 0), IsEnabled = false };
                Grid.SetRow(selectSchool, 1);
                Grid.SetColumn(selectSchool, 1);

                searchSchoolQuery.KeyDown += async (sender, e) =>
                {
                    if (e.Key == Key.Enter)
                    {
                        await OnSearchSchool(schoolComboBox, searchSchoolQuery, searchSchool, selectSchool);
                        searchSchoolQuery.Focus();
                    }
                };
                searchSchool.Click += async (sender, e) => await OnSearchSchool(schoolComboBox, searchSchoolQuery, searchSchool, selectSchool);
                selectSchool.Click += (sender, e) =>
                {
                    _school = (School)schoolComboBox.SelectedItem;
                    _school.WriteToFile();
                    OnLoaded(sender, e);
                };

                mainStackPanel.Children.Add(new Label { Content = "Search for School Name, City or Address", HorizontalAlignment = HorizontalAlignment.Center, Margin = new(0, 0, 0, 10) });
                mainStackPanel.Children.Add(new Grid
                {
                    Children = { searchSchoolQuery, searchSchool, schoolComboBox, selectSchool },
                    RowDefinitions =
                    {
                        new RowDefinition { Height = new(1, GridUnitType.Auto) },
                        new RowDefinition { Height = new(1, GridUnitType.Auto) }
                    },
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = new(1, GridUnitType.Auto) }
                    }
                });
            }
            else
            {
                _school = School.ReadFromFile();

                mainStackPanel.Children.Add(new Label { Content = _school.Name, HorizontalAlignment = HorizontalAlignment.Center, Margin = new(0, 0, 0, 10) });

                var username = new TextBox { MinWidth = 500, Margin = new(0, 0, 0, 10), HorizontalContentAlignment = HorizontalAlignment.Center };
                var password = new PasswordBox { MinWidth = 500, Margin = new(0, 0, 0, 20), HorizontalContentAlignment = HorizontalAlignment.Center };

                var login = new Button { Content = "Login" };
                login.Click += async (sender, e) =>
                {
                    username.IsEnabled = password.IsEnabled = login.IsEnabled = false;

                    _session = new(_school, username.Text, password.Password);
                    await _session.TryLogin();

                    if (_session.LoggedIn)
                    {
                        #region Settings
                        var settingsStackPanel = new StackPanel { Margin = new(0, 0, 50, 0) };
                        Grid.SetColumn(settingsStackPanel, 0);

                        foreach (var property in typeof(Settings).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => prop.GetCustomAttribute<JsonIgnoreAttribute>() == null))
                        {
                            var fieldName = new Label { Content = property.Name };
                            Grid.SetColumn(fieldName, 0);

                            var fieldInput = new TextBox
                            {
                                Text = (string?)property.GetValue(_settings),
                                Name = property.Name,
                                Margin = new(15, 0, 0, 0)
                            };
                            Grid.SetColumn(fieldInput, 1);

                            settingsStackPanel.Children.Add(new Grid
                            {
                                Children = { fieldName, fieldInput },
                                ColumnDefinitions =
                                {
                                    new ColumnDefinition { Width = new(1, GridUnitType.Star) },
                                    new ColumnDefinition { Width = new(1, GridUnitType.Auto) }
                                },
                                Margin = new(0, 0, 0, 10)
                            });
                        }

                        var saveSettings = new Button { Content = "Save Settings" };
                        saveSettings.Click += (sender, e) =>
                        {
                            settingsStackPanel.IsEnabled = false;
                            _settings = Settings.FromUIElementCollection(settingsStackPanel.Children);
                            _settings.WriteToFile();
                            settingsStackPanel.IsEnabled = true;
                        };
                        settingsStackPanel.Children.Add(saveSettings);
                        #endregion

                        #region Fetch Exams
                        var fetchExamsStackPanel = new StackPanel { Margin = new(50, 0, 0, 0) };
                        Grid.SetColumn(fetchExamsStackPanel, 1);

                        var dateTimeFrom = new TextBox
                        {
                            Text = new DateOnly(DateTime.Now.Year - (DateTime.Now.Month <= 8 ? 1 : 0), 9, 1).ToString(_settings.ShortDateFormat),
                            MinWidth = 200,
                            TextAlignment = TextAlignment.Center,
                            Margin = new(0, 0, 0, 10)
                        };

                        var dateTimeTo = new TextBox
                        {
                            Text = new DateOnly(DateTime.Now.Year + (DateTime.Now.Month <= 8 ? 0 : 1), 7, 8).ToString(_settings.ShortDateFormat),
                            MinWidth = 200,
                            TextAlignment = TextAlignment.Center,
                            Margin = new(0, 0, 0, 20)
                        };

                        var fetchExams = new Button { Content = "Fetch Exams" };
                        fetchExams.Click += async (sender, e) => await OnFetchExams(dateTimeFrom, dateTimeTo, fetchExams);

                        fetchExamsStackPanel.Children.Add(dateTimeFrom);
                        fetchExamsStackPanel.Children.Add(dateTimeTo);
                        fetchExamsStackPanel.Children.Add(fetchExams);
                        #endregion

                        mainStackPanel.Children.Clear();
                        mainStackPanel.Children.Add(new Grid
                        {
                            Children = { settingsStackPanel, fetchExamsStackPanel },
                            ColumnDefinitions =
                            {
                                new ColumnDefinition { Width = new(1, GridUnitType.Star) },
                                new ColumnDefinition { Width = new(1, GridUnitType.Star) }
                            }
                        });
                    }
                    else
                    {
                        MessageBox.Show("Failed to login!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        username.IsEnabled = password.IsEnabled = login.IsEnabled = true;
                    }
                };

                mainStackPanel.Children.Add(username);
                mainStackPanel.Children.Add(password);
                mainStackPanel.Children.Add(login);
            }

            Main.Content = mainStackPanel;
        }

        private async Task OnSearchSchool(ComboBox schoolsComboBox, TextBox searchSchoolQuery, Button searchSchool, Button selectSchool)
        {
            searchSchoolQuery.IsEnabled = searchSchool.IsEnabled = false;

            try
            {
                var schools = await Session.SearchSchool(searchSchoolQuery.Text);

                if (schools.Any())
                {
                    selectSchool.IsEnabled = true;
                    schoolsComboBox.ItemsSource = schools;
                }
                else { MessageBox.Show("No schools found!", "Info", MessageBoxButton.OK, MessageBoxImage.Information); }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            finally { searchSchoolQuery.IsEnabled = searchSchool.IsEnabled = true; }
        }

        private async Task OnFetchExams(TextBox dateTimeFrom, TextBox dateTimeTo, Button fetchExams)
        {
            UIElement[] elementsToDisable = { dateTimeFrom, dateTimeTo, fetchExams };

            foreach (var element in elementsToDisable) { element.IsEnabled = false; }
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                var from = DateOnly.ParseExact(dateTimeFrom.Text, _settings.ShortDateFormat, CultureInfo.InvariantCulture);
                var to = DateOnly.ParseExact(dateTimeTo.Text, _settings.ShortDateFormat, CultureInfo.InvariantCulture);

                _exams = await _session!.TryGetExams(from, to);

                if (_exams.Any())
                {
                    var mainStackPanel = (StackPanel)Main.Content;
                    mainStackPanel.Children.Clear();
                    mainStackPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
                    mainStackPanel.VerticalAlignment = VerticalAlignment.Stretch;

                    var saveExams = new Button { Content = "Save to File", Padding = new(12.5, 7.5, 12.5, 7.5), Margin = new(0, 0, 5, 0), HorizontalAlignment = HorizontalAlignment.Right };
                    Grid.SetColumn(saveExams, 0);
                    saveExams.Click += (sender, e) => OnSaveExams(mainStackPanel.Children.Cast<UIElement>().Skip(1));

                    var pushExams = new Button { Content = "Push to Google Calendar", Padding = new(12.5, 7.5, 12.5, 7.5), Margin = new(5, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
                    Grid.SetColumn(pushExams, 1);
                    pushExams.Click += OnPushExams;

                    mainStackPanel.Children.Add(new Grid
                    {
                        Children = { saveExams, pushExams },
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = new(1, GridUnitType.Star) },
                            new ColumnDefinition { Width = new(1, GridUnitType.Star) }
                        },
                        Margin = new(0, 0, 0, 20)
                    });

                    for (var i = 0; i < _exams.Count(); i++)
                    {
                        var exam = _exams.ElementAt(i);
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
                        toggleExam.Checked += (sender, e) => { subject.Foreground = date.Foreground = time.Foreground = Brushes.Black; };
                        toggleExam.Unchecked += (sender, e) => { subject.Foreground = date.Foreground = time.Foreground = Brushes.Gray; };
                        Grid.SetColumn(toggleExam, 2);

                        var grid = new Grid
                        {
                            Children = { subject, dateTime, toggleExam },
                            ColumnDefinitions =
                            {
                                new ColumnDefinition { Width = new(1, GridUnitType.Star) },
                                new ColumnDefinition { Width = new(1, GridUnitType.Star) },
                                new ColumnDefinition { Width = new(1, GridUnitType.Star) }
                            },
                            MinWidth = 400
                        };

                        mainStackPanel.Children.Add(new Border
                        {
                            Child = grid,
                            Margin = new(0, 10, 0, 10),
                            Padding = new(25, 17.5, 25, 17.5),
                            Background = i % 2 == 0 ? Brushes.White : Brushes.LightGray,
                            CornerRadius = new(5)
                        });
                    }
                }
                else
                {
                    MessageBox.Show("No exams found!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
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
                foreach (var element in elementsToDisable) { element.IsEnabled = true; }
                Mouse.OverrideCursor = null;
            }
        }

        // TODO: optimize by indexing exams instead of looping through _exams
        private void OnSaveExams(IEnumerable<UIElement> examUIElements)
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

                foreach (var examUIElement in examUIElements)
                {
                    var grid = (Grid)((Border)examUIElement).Child;
                    var isChecked = ((ToggleButton)grid.Children[2]).IsChecked;

                    if (isChecked!.Value)
                    {
                        var subject = (string)((Label)grid.Children[0]).Content;
                        var date = (string)((Label)((Grid)grid.Children[1]).Children[0]).Content;
                        var time = (string)((Label)((Grid)grid.Children[1]).Children[1]).Content;

                        foreach (var exam in _exams!)
                        {
                            if (exam.Subject == subject && exam.Date.ToString(_settings.LongDateFormat) == date && $"{exam.Start.ToString(_settings.TimeFormat)} - {exam.End.ToString(_settings.TimeFormat)}" == time)
                            {
                                writer.WriteLine($"{exam.Subject},{exam.Description},{exam.Date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)},{exam.StartTime:h:mm tt},{exam.EndTime:h:mm tt}");
                                break;
                            }
                        }
                    }
                }
            }
        }

        // TODO: implement asking for Calendar-ID + implement pushing to Google Calendar
        private void OnPushExams(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Push to Google Calendar");
        }
    }
}