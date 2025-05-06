using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging; // Aggiunto per il logging

namespace TaskRoute.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GeminiService> _logger; // Aggiunto per il logging

        // Modificato per iniettare ILogger
        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            // Corretta la chiave di configurazione per corrispondere a appsettings.json fornito ("Gemini:ApiKey")
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException("La chiave API Gemini (Gemini:ApiKey) non è stata trovata nella configurazione.");
            _logger = logger; // Inizializza il logger
        }

        public async Task<string> GetResponseAsync(string prompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                // Aggiunte configurazioni di generazione comuni, puoi personalizzarle o rimuoverle se non necessarie
                generationConfig = new
                {
                    temperature = 0.7, // Controlla la casualità, più basso = più deterministico
                    maxOutputTokens = 2048 // Massimo numero di token nella risposta
                }
            };

            var jsonRequestContent = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(jsonRequestContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Richiesta inviata a Gemini. URL: {Url}, Corpo: {Body}", url, jsonRequestContent);

            try
            {
                var response = await _httpClient.PostAsync(url, httpContent);

                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Errore dalla API Gemini. Status Code: {StatusCode}, Risposta: {ResponseBody}", response.StatusCode, responseBody);
                    // Lancia un'eccezione più specifica o gestisci l'errore come preferisci
                    // Per ora, rilanciamo un'eccezione generica ma con il messaggio di errore dall'API se disponibile
                    throw new HttpRequestException($"Errore dalla API Gemini: {response.StatusCode}. Dettagli: {responseBody}");
                }

                _logger.LogInformation("Risposta ricevuta da Gemini: {ResponseBody}", responseBody);

                // Parsing della risposta JSON per estrarre il testo generato
                using (JsonDocument doc = JsonDocument.Parse(responseBody))
                {
                    JsonElement candidatesElement;
                    if (doc.RootElement.TryGetProperty("candidates", out candidatesElement) && candidatesElement.ValueKind == JsonValueKind.Array && candidatesElement.GetArrayLength() > 0)
                    {
                        JsonElement firstCandidate = candidatesElement[0];
                        JsonElement contentElement;
                        if (firstCandidate.TryGetProperty("content", out contentElement) && contentElement.TryGetProperty("parts", out JsonElement partsElement) && partsElement.ValueKind == JsonValueKind.Array && partsElement.GetArrayLength() > 0)
                        {
                            JsonElement firstPart = partsElement[0];
                            JsonElement textElement;
                            if (firstPart.TryGetProperty("text", out textElement))
                            {
                                return textElement.GetString() ?? string.Empty;
                            }
                        }
                    }
                    // Se la struttura non è quella attesa, logga un errore e restituisci una stringa vuota o un messaggio di errore
                    _logger.LogError("Struttura JSON della risposta di Gemini non conforme alle aspettative. Risposta: {ResponseBody}", responseBody);
                    throw new JsonException("Struttura JSON della risposta di Gemini non conforme alle aspettative.");
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Errore HttpRequestException durante la chiamata a Gemini.");
                throw; // Rilancia l'eccezione per essere gestita dal chiamante (Index.cshtml.cs)
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Errore JsonException durante il parsing della risposta di Gemini.");
                throw; // Rilancia l'eccezione
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore generico in GetResponseAsync durante la chiamata a Gemini.");
                throw; // Rilancia l'eccezione
            }
        }
    }
}

