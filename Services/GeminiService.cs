using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace TaskRoute.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GeminiApi:ApiKey"] ?? throw new InvalidOperationException("Gemini API key non trovata.");
        }

        /// <summary>
        /// Invia una richiesta all'API di Gemini e restituisce la risposta.
        /// </summary>
        /// <param name="prompt">Il testo da inviare all'API.</param>
        /// <returns>La risposta dell'API come stringa.</returns>
        public async Task<string> GetResponseAsync(string prompt)
        {
            // URL dell'endpoint API di Gemini
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";

            // Creazione del corpo della richiesta
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = prompt
                            }
                        }
                    }
                }
            };

            // Serializzazione del corpo in JSON
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            // Aggiunta dell'intestazione di autorizzazione
            _httpClient.DefaultRequestHeaders.Clear();

            // Invio della richiesta HTTP POST
            var response = await _httpClient.PostAsync(url, jsonContent);

            // Verifica dello stato della risposta
            response.EnsureSuccessStatusCode();

            // Lettura della risposta come stringa
            var responseBody = await response.Content.ReadAsStringAsync();

            return responseBody;
        }
    }
}