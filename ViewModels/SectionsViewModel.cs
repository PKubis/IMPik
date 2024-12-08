using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using IMP.Models;
using IMP.Services;
using Microsoft.Maui.Controls;

namespace IMP.ViewModels
{
    public class SectionsViewModel : BaseViewModel
    {
        private readonly RealtimeDatabaseService _firebaseService;
        private string _userId;

        // Publiczny konstruktor wymagany przez XAML
        public SectionsViewModel()
        {
            _firebaseService = new RealtimeDatabaseService();
            Sections = new ObservableCollection<Section>();
            InitializeDayColors();

            ToggleDayCommand = new Command<string>(ToggleDay);
            AddSectionCommand = new Command(async () => await AddSection());
            DeleteSectionCommand = new Command<string>(async id => await DeleteSection(id));
            EditSectionCommand = new Command<string>(async id => await EditSection(id));
        }

        public SectionsViewModel(string userId) : this()
        {
            _userId = userId;
            LoadSectionsAsync();
        }

        public ObservableCollection<Section> Sections { get; set; }
        public Dictionary<string, string> DayColors { get; set; }

        public ICommand ToggleDayCommand { get; }
        public ICommand AddSectionCommand { get; }
        public ICommand DeleteSectionCommand { get; }
        public ICommand EditSectionCommand { get; }

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

        private readonly List<string> _selectedDays = new();

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
            OnPropertyChanged(nameof(DayColors));
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


        private async Task LoadSectionsAsync()
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
            InitializeDayColors(); // Resetuj wybór dni tygodnia
            OnPropertyChanged(nameof(Sections));
        }

        private async Task DeleteSection(string sectionId)
        {
            var section = Sections.FirstOrDefault(s => s.Id == sectionId);
            if (section == null) return;

            bool confirm = await Application.Current.MainPage.DisplayAlert("Usuń sekcję", $"Czy na pewno chcesz usunąć sekcję \"{section.Name}\"?", "Tak", "Nie");
            if (!confirm) return;

            await _firebaseService.DeleteSectionAsync(_userId, sectionId);
            Sections.Remove(section);
        }

        private async Task EditSection(string sectionId)
        {
            var section = Sections.FirstOrDefault(s => s.Id == sectionId);
            if (section == null) return;

            string newName = await Application.Current.MainPage.DisplayPromptAsync("Edytuj sekcję", "Podaj nową nazwę sekcji:", initialValue: section.Name);
            if (string.IsNullOrWhiteSpace(newName)) return;

            string newStartTime = await Application.Current.MainPage.DisplayPromptAsync("Edytuj czas rozpoczęcia", "Podaj nowy czas rozpoczęcia (HH:mm):", initialValue: section.StartTime);
            if (string.IsNullOrWhiteSpace(newStartTime)) return;

            string newDuration = await Application.Current.MainPage.DisplayPromptAsync("Edytuj czas trwania", "Podaj nowy czas trwania (minuty):", initialValue: section.Duration.ToString());
            if (string.IsNullOrWhiteSpace(newDuration)) return;

            var newDays = await Application.Current.MainPage.DisplayPromptAsync("Edytuj dni", "Podaj nowe dni tygodnia oddzielone przecinkami (np. pn, wt, śr):", initialValue: section.SelectedDays);
            if (string.IsNullOrWhiteSpace(newDays)) return;

            section.Name = newName;
            section.StartTime = newStartTime;
            section.Duration = int.Parse(newDuration);
            section.SelectedDays = newDays;

            await _firebaseService.SaveSectionAsync(_userId, section);
            Sections[Sections.IndexOf(section)] = section;
        }
    }
}
