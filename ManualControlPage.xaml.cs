using IMP.Models;
using IMP.Services;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace IMP
{
    public partial class ManualControlPage : ContentPage
    {
        public ObservableCollection<Section> Sections { get; set; } = new ObservableCollection<Section>();

        private readonly string _userId;
        private readonly RealtimeDatabaseService _databaseService;
        private readonly Dictionary<string, System.Timers.Timer> _timers = new Dictionary<string, System.Timers.Timer>();

        public Command<string> StartCommand { get; }
        public Command<string> StopCommand { get; }

        public ManualControlPage(string userId)
        {
            InitializeComponent();

            _userId = userId;
            _databaseService = new RealtimeDatabaseService();

            StartCommand = new Command<string>(StartTimer);
            StopCommand = new Command<string>(StopTimer);

            BindingContext = this;

            LoadSectionsAsync(); // Pobranie danych
        }

        private async Task LoadSectionsAsync()
        {
            var sections = await _databaseService.GetSectionsAsync(_userId);
            Sections.Clear();
            foreach (var section in sections)
            {
                section.ElapsedTime = 0; // Reset czasu
                Sections.Add(section);
            }
        }

        private void StartTimer(string sectionId)
        {
            // Zatrzymaj dzia³aj¹cy timer, jeœli istnieje
            if (_timers.TryGetValue(sectionId, out var existingTimer))
            {
                existingTimer.Stop();
                existingTimer.Dispose();
                _timers.Remove(sectionId);
            }

            // Resetuj licznik czasu
            var section = Sections.FirstOrDefault(sec => sec.Id == sectionId);
            if (section == null) return;

            section.ElapsedTime = 0; // Reset czasu do zera
            Device.BeginInvokeOnMainThread(() =>
            {
                // Aktualizacja UI
                var updatedSection = Sections.First(sec => sec.Id == sectionId);
                Sections[Sections.IndexOf(updatedSection)] = section;
            });

            // Rozpocznij nowy timer
            var timer = new System.Timers.Timer(1000); // Timer co sekundê
            timer.Elapsed += (s, e) =>
            {
                if (section != null)
                {
                    section.ElapsedTime++;
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        // Aktualizacja UI
                        var updatedSection = Sections.First(sec => sec.Id == sectionId);
                        Sections[Sections.IndexOf(updatedSection)] = section;
                    });
                }
            };
            timer.Start();
            _timers[sectionId] = timer;
        }

        private void StopTimer(string sectionId)
        {
            var section = Sections.FirstOrDefault(sec => sec.Id == sectionId);
            if (section == null) return;

            // Jeœli timer dzia³a, zatrzymaj go
            if (_timers.TryGetValue(sectionId, out var timer))
            {
                timer.Stop();
                timer.Dispose();
                _timers.Remove(sectionId);

                // Zapisz stan "zatrzymany"
                section.ElapsedTime = section.ElapsedTime; // Bez resetowania
            }
            else
            {
                // Jeœli timer ju¿ zatrzymany, wyzeruj czas
                section.ElapsedTime = 0;
                Device.BeginInvokeOnMainThread(() =>
                {
                    // Aktualizacja UI
                    var updatedSection = Sections.First(sec => sec.Id == sectionId);
                    Sections[Sections.IndexOf(updatedSection)] = section;
                });
            }
        }
    }
}
