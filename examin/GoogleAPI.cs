using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace examin.GoogleAPI
{
    internal class Calendar(in Config.Calendar _config)
    {
        private readonly Config.Calendar config = _config;
        private CalendarService? _service;
        private HashSet<Exam> _exams = [];

        public void Authorize()
        {
            using var stream = new FileStream(Config.Calendar.ClientSecretFile, FileMode.Open, FileAccess.Read);
            var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                [CalendarService.Scope.CalendarEvents],
                "user",
                CancellationToken.None,
                new FileDataStore("Daimto.GoogleCalendar.Auth.Store")
            ).Result;

            _service = new CalendarService(new BaseClientService.Initializer { HttpClientInitializer = credential });
        }

        public void Deauthorize()
        {
            var tokenPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Daimto.GoogleCalendar.Auth.Store");

            if (Directory.Exists(tokenPath)) { Directory.Delete(tokenPath, true); }
            _service = null;
        }

        public async Task<IEnumerable<Exam>> GetExams()
        {
            if (_service == null) { throw new InvalidOperationException("Service not authenticated"); }

            var request = _service.Events.List(config.CalendarID);
            request.TimeMinDateTimeOffset = DateTimeOffset.Now;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var events = await request.ExecuteAsync();
            var exams = events.Items.Select(Exam.FromGoogleEvent);

            _exams.UnionWith(exams);
            return _exams;
        }

        public bool AddExam(Exam exam) => _exams.Add(exam);

        public void PushExams()
        {
            if (_service == null) { throw new InvalidOperationException("Service not authenticated"); }
            var calendarId = $"{config.CalendarID}@group.calendar.google.com";

            var existingEventsRequest = _service.Events.List(calendarId);
            existingEventsRequest.SingleEvents = true;
            existingEventsRequest.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
            var existingEvents = existingEventsRequest.Execute().Items;

            foreach (var exam in _exams)
            {
                var googleEvent = exam.ToGoogleEvent();
                var existingEventsMatch = existingEvents.FirstOrDefault(existingEvent => existingEvent.Summary == exam.Subject && DateOnly.FromDateTime(existingEvent.Start.DateTimeDateTimeOffset!.Value.Date) == exam.Date);

                if (existingEventsMatch is not null)
                {
                    if (existingEventsMatch.Start.DateTimeDateTimeOffset != exam.Start || existingEventsMatch.End.DateTimeDateTimeOffset != exam.End)
                    {
                        var updateEventRequest = _service.Events.Update(googleEvent, calendarId, existingEventsMatch.Id);
                        updateEventRequest.Execute();
                    }
                }
                else { _service.Events.Insert(googleEvent, calendarId).Execute(); }
            }
        }
    }
}
