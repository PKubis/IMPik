using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IMP.Models;

namespace IMP.Services
{
    public class RealtimeDatabaseService
    {
        private readonly string _databaseUrl = "https://impdb-557fa-default-rtdb.europe-west1.firebasedatabase.app/";
        private readonly HttpClient _httpClient;

        public RealtimeDatabaseService()
        {
            _httpClient = new HttpClient();
        }

        // Metoda zapisu sekcji do Firebase
        public async Task SaveSectionAsync(string userId, Section section)
        {
            try
            {
                var json = JsonSerializer.Serialize(section);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{_databaseUrl}users/{userId}/sections/{section.Id}.json"; // Każda sekcja zapisywana pod unikalnym ID
                var response = await _httpClient.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to save section: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving section: {ex.Message}");
                throw;
            }
        }

        // Metoda odczytu sekcji z Firebase
        public async Task<List<Section>> GetSectionsAsync(string userId)
        {
            try
            {
                var url = $"{_databaseUrl}users/{userId}/sections.json";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to fetch sections: {response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json) || json == "null")
                    return new List<Section>();

                var sectionsDict = JsonSerializer.Deserialize<Dictionary<string, Section>>(json);
                return sectionsDict?.Values.ToList() ?? new List<Section>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching sections: {ex.Message}");
                return new List<Section>();
            }
        }

        // Metoda zapisu użytkownika do Firebase
        public async Task AddUserAsync(string userId, string email)
        {
            try
            {
                var user = new
                {
                    Email = email,
                    CreatedAt = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(user);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{_databaseUrl}users/{userId}.json";
                var response = await _httpClient.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error adding user: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
        }
    }
}
