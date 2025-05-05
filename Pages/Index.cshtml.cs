using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TaskRoute.Data;
using TaskRoute.Models;

namespace TaskRoute.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<IndexModel> _logger;
        private readonly string _apiKey = "AIzaSyBWu31eHDDU8vo01g4czH5kWbTuKLCbCe0"; // User provided API Key

        public IndexModel(ApplicationDbContext context, IHttpClientFactory clientFactory, ILogger<IndexModel> logger)
        {
            _context = context;
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public List<Commission> Commissions { get; set; }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Commissions = await _context.Commissions
                .Include(c => c.Location)
                .Where(c => c.UserId == userId && !c.IsCompleted)
                .OrderBy(c => c.DueDate) // Optionally order by due date initially
                .ToListAsync();
        }

        // Represents a task location for optimization
        public class Waypoint
        {
            public int Id { get; set; }
            public double Lat { get; set; }
            public double Lng { get; set; }
            public string Title { get; set; }
        }

        // --- Gemini API Request/Response Structures ---
        private class GeminiRequestPart
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }
        }

        private class GeminiRequestContent
        {
            [JsonPropertyName("parts")]
            public List<GeminiRequestPart> Parts { get; set; }
        }

        private class GeminiRequest
        {
            [JsonPropertyName("contents")]
            public List<GeminiRequestContent> Contents { get; set; }
        }

        private class GeminiResponsePart
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }
        }

        private class GeminiResponseContent
        {
            [JsonPropertyName("parts")]
            public List<GeminiResponsePart> Parts { get; set; }
            [JsonPropertyName("role")]
            public string Role { get; set; }
        }

        private class GeminiResponseCandidate
        {
            [JsonPropertyName("content")]
            public GeminiResponseContent Content { get; set; }
            // Add other potential fields like finishReason, safetyRatings if needed
        }

        private class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public List<GeminiResponseCandidate> Candidates { get; set; }
            // Add promptFeedback if needed
        }

        // Structure expected within the Gemini response text (as JSON)
        private class OptimizationResult
        {
            [JsonPropertyName("ordered_titles")]
            public List<string> OrderedTitles { get; set; }
            [JsonPropertyName("motivation")]
            public string Motivation { get; set; }
            [JsonPropertyName("estimated_minutes")]
            public int EstimatedMinutes { get; set; }
        }
        // ---------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken] // Enable AntiForgery Token validation
        public async Task<IActionResult> OnPostOptimizeAsync([FromBody] List<int> selectedIds)
        {
            if (selectedIds == null || !selectedIds.Any())
            {
                return BadRequest(new { error = "Nessun ID di commissione selezionato." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var commissions = await _context.Commissions
                    .Include(c => c.Location)
                    .Where(c => selectedIds.Contains(c.Id) && c.UserId == userId && !c.IsCompleted)
                    .ToListAsync();

                if (commissions.Count < 2)
                {
                    return BadRequest(new { error = "Seleziona almeno due commissioni per l'ottimizzazione." });
                }

                var waypoints = commissions.Select(c => new Waypoint
                {
                    Id = c.Id,
                    Title = c.Title,
                    // Ensure Location is not null, handle potential nulls gracefully
                    Lat = c.Location?.Latitude ?? 0, // Use 0 or throw if location is mandatory
                    Lng = c.Location?.Longitude ?? 0
                }).ToList();

                // Check for invalid coordinates (if 0,0 is invalid)
                if (waypoints.Any(w => w.Lat == 0 && w.Lng == 0))
                {
                    _logger.LogWarning("Alcune commissioni selezionate non hanno coordinate valide.");
                    // Decide whether to proceed or return an error
                    // return BadRequest(new { error = "Alcune commissioni selezionate non hanno coordinate valide." });
                }

                // --- Call Gemini API --- 
                var client = _clientFactory.CreateClient();
                string endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key={_apiKey}"; // Using v1beta and a specific model

                // Improved prompt asking for JSON output
                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine("Dato il seguente elenco di commissioni con i loro titoli e coordinate GPS (latitudine, longitudine):");
                foreach (var wp in waypoints)
                {
                    promptBuilder.AppendLine($"- Titolo: '{wp.Title}', Coordinate: {wp.Lat},{wp.Lng}");
                }
                promptBuilder.AppendLine("\nPer favore, ottimizza l'ordine di visita di queste commissioni per minimizzare il tempo di viaggio totale. Considera un punto di partenza ragionevole se non specificato.");
                promptBuilder.AppendLine("Fornisci la risposta ESCLUSIVAMENTE in formato JSON valido con la seguente struttura:");
                promptBuilder.AppendLine("{");
                promptBuilder.AppendLine("  \"ordered_titles\": [\"Titolo commissione 1 nell'ordine ottimale\", \"Titolo commissione 2\", ...], // Lista dei titoli ESATTI nell'ordine ottimizzato");
                promptBuilder.AppendLine("  \"motivation\": \"Spiegazione concisa (2-3 frasi) del perché questo ordine è ottimale...\",");
                promptBuilder.AppendLine("  \"estimated_minutes\": N // Stima numerica del tempo totale in minuti (solo il numero)");
                promptBuilder.AppendLine("}");
                promptBuilder.AppendLine("Assicurati che i titoli in 'ordered_titles' corrispondano ESATTAMENTE a quelli forniti nell'input.");

                var requestPayload = new GeminiRequest
                {
                    Contents = new List<GeminiRequestContent>
                    {
                        new GeminiRequestContent { Parts = new List<GeminiRequestPart> { new GeminiRequestPart { Text = promptBuilder.ToString() } } }
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(requestPayload);
                var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogInformation("Chiamata API Gemini all'endpoint: {Endpoint}", endpoint);
                // _logger.LogDebug("Payload Gemini: {Payload}", jsonPayload); // Log payload only if necessary for debugging

                HttpResponseMessage httpResponse = await client.PostAsync(endpoint, httpContent);

                var responseBody = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Errore nella chiamata API Gemini. Status: {StatusCode}, Response: {ResponseBody}", httpResponse.StatusCode, responseBody);
                    return StatusCode((int)httpResponse.StatusCode, new { error = $"Errore durante la comunicazione con il servizio di ottimizzazione (Codice: {httpResponse.StatusCode}). Dettagli: {responseBody}" });
                }

                _logger.LogInformation("Risposta API Gemini ricevuta con successo.");
                // _logger.LogDebug("Response Body Gemini: {ResponseBody}", responseBody); // Log response body only if necessary

                // --- Parse Gemini Response --- 
                GeminiResponse geminiResponse;
                try
                {
                    geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Errore durante il parsing della risposta JSON esterna di Gemini: {ResponseBody}", responseBody);
                    return StatusCode(500, new { error = "Formato risposta esterna del servizio di ottimizzazione non valido." });
                }

                if (geminiResponse?.Candidates == null || !geminiResponse.Candidates.Any() || geminiResponse.Candidates[0].Content?.Parts == null || !geminiResponse.Candidates[0].Content.Parts.Any())
                {
                    _logger.LogError("Struttura della risposta di Gemini non valida o vuota: {ResponseBody}", responseBody);
                    return StatusCode(500, new { error = "Risposta del servizio di ottimizzazione incompleta o non valida." });
                }

                string generatedText = geminiResponse.Candidates[0].Content.Parts[0].Text;
                _logger.LogInformation("Testo generato da Gemini: {GeneratedText}", generatedText);

                // Clean the text if it's wrapped in markdown code blocks
                generatedText = generatedText.Trim().Trim('`');
                if (generatedText.StartsWith("json"))
                {
                    generatedText = generatedText.Substring(4).Trim();
                }

                OptimizationResult optimizationResult;
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }; // Be flexible with casing
                    optimizationResult = JsonSerializer.Deserialize<OptimizationResult>(generatedText, options);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Errore durante il parsing del JSON interno generato da Gemini: {GeneratedText}", generatedText);
                    return StatusCode(500, new { error = "Impossibile interpretare l'ordine ottimizzato ricevuto. Il formato potrebbe essere cambiato." });
                }

                if (optimizationResult?.OrderedTitles == null || optimizationResult.OrderedTitles.Count != waypoints.Count)
                {
                    _logger.LogError("L'ordine dei titoli ricevuto da Gemini non corrisponde al numero di task inviati. Ricevuto: {ReceivedTitles}, Atteso: {ExpectedCount}. Testo: {GeneratedText}",
                       optimizationResult?.OrderedTitles?.Count, waypoints.Count, generatedText);
                    return StatusCode(500, new { error = "L'ordine ottimizzato ricevuto non include tutte le commissioni selezionate." });
                }

                // --- Reorder Waypoints based on Gemini's response --- 
                var orderedWaypoints = new List<Waypoint>();
                var waypointDict = waypoints.ToDictionary(w => w.Title, w => w); // For quick lookup

                foreach (var title in optimizationResult.OrderedTitles)
                {
                    if (waypointDict.TryGetValue(title, out var waypoint))
                    {
                        orderedWaypoints.Add(waypoint);
                    }
                    else
                    {
                        // Try a normalized comparison if exact match fails
                        var normalizedTitle = NormalizeString(title);
                        var matchedWaypoint = waypoints.FirstOrDefault(w => NormalizeString(w.Title) == normalizedTitle);
                        if (matchedWaypoint != null)
                        {
                            _logger.LogWarning("Titolo '{OriginalTitle}' da Gemini trovato tramite matching normalizzato come '{MatchedTitle}'.", title, matchedWaypoint.Title);
                            orderedWaypoints.Add(matchedWaypoint);
                        }
                        else
                        {
                            _logger.LogError("Titolo '{Title}' ricevuto da Gemini non trovato tra le commissioni originali. Testo: {GeneratedText}", title, generatedText);
                            return StatusCode(500, new { error = $"Il servizio di ottimizzazione ha restituito un titolo non valido ('{title}')." });
                        }
                    }
                }

                // Ensure all original waypoints are included in the final list
                if (orderedWaypoints.Count != waypoints.Count)
                {
                    _logger.LogError("Errore nella ricostruzione dell'ordine: il numero finale di waypoint ({FinalCount}) non corrisponde a quello iniziale ({InitialCount}).", orderedWaypoints.Count, waypoints.Count);
                    return StatusCode(500, new { error = "Errore interno durante la finalizzazione dell'ordine del percorso." });
                }

                _logger.LogInformation("Percorso ottimizzato generato con successo.");

                return new JsonResult(new
                {
                    waypoints = orderedWaypoints.Select(w => new { w.Lat, w.Lng, w.Title }), // Return only necessary info
                    estimatedMinutes = optimizationResult.EstimatedMinutes > 0 ? optimizationResult.EstimatedMinutes : orderedWaypoints.Count * 7 + 10, // Fallback estimation
                    motivation = optimizationResult.Motivation ?? "Ordine ottimizzato per ridurre i tempi di percorrenza."
                });
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Errore HTTP durante la chiamata API Gemini.");
                return StatusCode(503, new { error = "Servizio di ottimizzazione temporaneamente non disponibile (Errore di rete)." });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Errore durante l'accesso al database.");
                return StatusCode(500, new { error = "Errore durante il recupero dei dati delle commissioni." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore imprevisto durante l'ottimizzazione del percorso.");
                return StatusCode(500, new { error = "Si è verificato un errore imprevisto durante l'ottimizzazione." });
            }
        }

        // Helper for case-insensitive, non-alphanumeric comparison
        private string NormalizeString(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return new string(input.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        }
    }
}

