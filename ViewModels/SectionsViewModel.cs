using System.Collections.ObjectModel;
using System.Windows.Input;
using IMP.Models;
using IMP.Services;

namespace IMP.ViewModels
{
    public class SectionsViewModel : BaseViewModel
    {
        private readonly RealtimeDatabaseService _firebaseService;
        private string _userId;

        public SectionsViewModel()
        {
            _firebaseService = new RealtimeDatabaseService();
            Sections = new ObservableCollection<Section>();
            InitializeDayColors();
            ToggleDayCommand = new Command<string>(ToggleDay);
            AddSectionCommand = new Command(async () => await AddSection());
        }

        public SectionsViewModel(string userId) : this()
        {
            _userId = userId;
            LoadSections();
        }

        public ObservableCollection<Section> Sections { get; set; }
        public Dictionary<string, string> DayColors { get; set; }

        public ICommand ToggleDayCommand { get; }
        public ICommand AddSectionCommand { get; }

        private string _sectionName;
        public string SectionName
        {
            get => _sectionName;
            set => SetProperty(ref _sectionName, value);
        }

        private string _startTime;
        public string StartTime
        {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        private string _duration;
        public string Duration
        {
            get => _duration;
            set
            {
                if (int.TryParse(value, out int result))
                {
                    _duration = result.ToString();
                    SetProperty(ref _duration, _duration);
                }
            }
        }

        private List<string> _selectedDays = new List<string>();

        private void InitializeDayColors()
        {
            DayColors = new Dictionary<string, string>
            {
                { "pn", "LightGray" },
                { "wt", "LightGray" },
                { "śr", "LightGray" },
                { "cz", "LightGray" },
                { "pt", "LightGray" },
                { "sb", "LightGray" },
                { "nd", "LightGray" }
            };
        }

        private void ToggleDay(string day)
        {
            if (_selectedDays.Contains(day))
            {
                _selectedDays.Remove(day);
                DayColors[day] = "LightGray";
            }
            else
            {
                _selectedDays.Add(day);
                DayColors[day] = "Teal";
            }
            OnPropertyChanged(nameof(DayColors));
        }

        private async void LoadSections()
        {
            if (string.IsNullOrEmpty(_userId)) return;

            var sections = await _firebaseService.GetSectionsAsync(_userId);
            Sections.Clear();
            foreach (var section in sections)
            {
                Sections.Add(section);
            }
        }

        private async Task AddSection()
        {
            if (string.IsNullOrWhiteSpace(SectionName) || string.IsNullOrWhiteSpace(StartTime) || string.IsNullOrWhiteSpace(Duration))
                return;

            var newSection = new Section
            {
                Id = Guid.NewGuid().ToString(),
                Name = SectionName,
                StartTime = StartTime,
                Duration = int.Parse(Duration),
                SelectedDays = string.Join(", ", _selectedDays)
            };

            await _firebaseService.SaveSectionAsync(_userId, newSection);
            Sections.Add(newSection);

            SectionName = string.Empty;
            StartTime = string.Empty;
            Duration = string.Empty;
            _selectedDays.Clear();
            InitializeDayColors();
            OnPropertyChanged(nameof(DayColors));
            OnPropertyChanged(nameof(Sections));
        }
    }
}
