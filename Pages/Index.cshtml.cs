using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TaskRoute.Data;
using TaskRoute.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net.Http;
using System;
using TaskRoute.Services; // Assumendo che GeminiService sia qui

namespace TaskRoute.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;
        private readonly GeminiService _geminiService; // Inietta il servizio Gemini

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger, GeminiService geminiService)
        {
            _context = context;
            _logger = logger;
            _geminiService = geminiService;
        }

        public List<Commission> Commissions { get; set; }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Commissions = await _context.Commissions
                .Where(c => c.UserId == userId && !c.IsCompleted)
                .Include(c => c.Location) // Assicurati che la Location sia caricata
                .OrderBy(c => c.DueDate)  // Ordina per data di scadenza
                .ThenBy(c => c.SpecificTime) // Poi per orario specifico
                .ToListAsync();
        }

        // Struttura per i dati inviati a Gemini e per la sua risposta
        private class WaypointInfo
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public double Lat { get; set; }
            public double Lng { get; set; }
            public string DueDate { get; set; }
            public string SpecificTime { get; set; } // Formato HH:mm
            public int? EstimatedDurationMinutes { get; set; }
        }

        private class OptimizationResult
        {
            public List<string> OrderedTitles { get; set; }
            public int EstimatedTotalMinutes { get; set; } // Modificato da EstimatedMinutes
            public string Motivation { get; set; }
        }

        public async Task<IActionResult> OnPostOptimizeAsync([FromBody] List<int> selectedTaskIds)
        {
            if (selectedTaskIds == null || selectedTaskIds.Count < 2)
            {
                return BadRequest(new { error = "Seleziona almeno due commissioni per l'ottimizzazione." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var selectedCommissions = await _context.Commissions
                .Where(c => c.UserId == userId && selectedTaskIds.Contains(c.Id) && c.LocationId.HasValue)
                .Include(c => c.Location)
                .ToListAsync();

            if (selectedCommissions.Count < 2)
            {
                return BadRequest(new { error = "Almeno due delle commissioni selezionate devono avere una posizione valida per l'ottimizzazione." });
            }

            var waypoints = selectedCommissions.Select(c => new WaypointInfo
            {
                Id = c.Id,
                Title = c.Title,
                Lat = c.Location.Latitude,
                Lng = c.Location.Longitude,
                DueDate = c.DueDate.ToString("yyyy-MM-dd"),
                SpecificTime = c.SpecificTime?.ToString("hh\\:mm"), // Formato HH:mm, es. 14:30
                EstimatedDurationMinutes = c.EstimatedDurationMinutes // Rimosso il segno di chiusura errato
            }).ToList(); // Aggiunto il punto e virgola e chiuso correttamente la chiamata LINQ


            // Costruzione del prompt per Gemini
            var tasksString = string.Join("; ", waypoints.Select(w =>
                $"Task: '{w.Title}', Posizione: ({w.Lat},{w.Lng}), Data Scadenza: {w.DueDate}" +
                (w.SpecificTime != null ? $", Orario Specifico: {w.SpecificTime}" : "") +
                (w.EstimatedDurationMinutes.HasValue ? $", Durata Svolgimento Stimata: {w.EstimatedDurationMinutes} minuti" : "")
            ));

            var prompt = $"Sei un assistente per l'ottimizzazione di percorsi e gestione del tempo. Considera i seguenti task: " +
                         tasksString + ". " +
                         "Devi fornire l'ordine ottimale per visitarli. Per ogni task, se è specificata una 'Durata Svolgimento Stimata', questa è il tempo da passare sul posto. " +
                         "Devi stimare i tempi di viaggio tra le località basandoti sulle loro coordinate geografiche. " +
                         "L'obiettivo è minimizzare il tempo totale, che include sia i tempi di viaggio sia i tempi di svolgimento delle attività. " +
                         "Se sono specificati 'Orari Specifici', questi dovrebbero essere rispettati il più possibile o considerati come vincoli temporali. " +
                         "Rispondi ESCLUSIVAMENTE con un oggetto JSON strutturato così: " +
                         "{{\"OrderedTitles\": [\"Titolo Task Ordinato 1\", \"Titolo Task Ordinato 2\", ...], \"EstimatedTotalMinutes\": MINUTI_TOTALI_STIMATI_REALISTICI, \"Motivation\": \"Testo della motivazione dettagliata...\"}}. " +
                         "Assicurati che 'OrderedTitles' contenga esattamente gli stessi titoli dei task forniti, solo riordinati. " +
                         "'EstimatedTotalMinutes' deve essere un numero intero che rappresenta la stima realistica del tempo totale necessario (viaggi + attività). " +
                         "'Motivation' deve spiegare chiaramente perché questo ordine è efficiente, come tiene conto degli orari (se presenti), delle durate delle attività e dei tempi di viaggio stimati.";

            _logger.LogInformation("Prompt per Gemini: {Prompt}", prompt);

            try
            {
                // Update the method call to use the correct method name from GeminiService
                var generatedText = await _geminiService.GetResponseAsync(prompt);

                _logger.LogInformation("Risposta da Gemini: {GeneratedText}", generatedText);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                OptimizationResult optimizationResult = null;
                try
                {
                    // Tentativo di pulire la risposta di Gemini se include ```json ... ```
                    var cleanedJson = generatedText.Trim();
                    if (cleanedJson.StartsWith("```json"))
                    {
                        cleanedJson = cleanedJson.Substring(7);
                    }
                    if (cleanedJson.EndsWith("```"))
                    {
                        cleanedJson = cleanedJson.Substring(0, cleanedJson.Length - 3);
                    }
                    cleanedJson = cleanedJson.Trim();

                    optimizationResult = JsonSerializer.Deserialize<OptimizationResult>(cleanedJson, options);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Errore durante il parsing del JSON da Gemini: {GeneratedText}", generatedText);
                    return StatusCode(500, new { error = "Impossibile interpretare la risposta del servizio di ottimizzazione. Formato non valido." });
                }

                if (optimizationResult?.OrderedTitles == null || optimizationResult.OrderedTitles.Count != waypoints.Count)
                {
                    _logger.LogError("L'ordine dei titoli da Gemini non corrisponde. Ricevuto: {ReceivedTitles}, Atteso: {ExpectedCount}. Testo: {GeneratedText}",
                       optimizationResult?.OrderedTitles?.Count, waypoints.Count, generatedText);
                    return StatusCode(500, new { error = "L'ordine ottimizzato ricevuto non include tutte le commissioni selezionate o è malformato." });
                }

                var orderedWaypoints = new List<WaypointInfo>();
                var waypointDict = waypoints.ToDictionary(w => w.Title, w => w, StringComparer.OrdinalIgnoreCase); // Usa IgnoreCase per il matching

                foreach (var titleFromGemini in optimizationResult.OrderedTitles)
                {
                    // Tentativo di match esatto o normalizzato
                    var matchedWaypoint = waypoints.FirstOrDefault(w => NormalizeString(w.Title) == NormalizeString(titleFromGemini));

                    if (matchedWaypoint != null)
                    {
                        orderedWaypoints.Add(matchedWaypoint);
                    }
                    else
                    {
                        _logger.LogError("Titolo '{Title}' da Gemini non trovato tra le commissioni originali. Originali: {OriginalTitles}. Testo Gemini: {GeneratedText}",
                            titleFromGemini, string.Join(", ", waypoints.Select(w => w.Title)), generatedText);
                        return StatusCode(500, new { error = $"Il servizio di ottimizzazione ha restituito un titolo non corrispondente ('{titleFromGemini}')." });
                    }
                }

                if (orderedWaypoints.Count != waypoints.Count)
                {
                    _logger.LogError("Errore nella ricostruzione dell'ordine: il numero finale di waypoint ({FinalCount}) non corrisponde a quello iniziale ({InitialCount}).", orderedWaypoints.Count, waypoints.Count);
                    return StatusCode(500, new { error = "Errore interno durante la finalizzazione dell'ordine del percorso." });
                }

                _logger.LogInformation("Percorso ottimizzato generato con successo.");
                return new JsonResult(new
                {
                    waypoints = orderedWaypoints.Select(w => new { w.Lat, w.Lng, w.Title }),
                    estimatedTotalMinutes = optimizationResult.EstimatedTotalMinutes, // Modificato qui
                    motivation = optimizationResult.Motivation ?? "Ordine ottimizzato per efficienza."
                });
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Errore HTTP durante la chiamata API Gemini.");
                return StatusCode(503, new { error = "Servizio di ottimizzazione temporaneamente non disponibile (Errore di rete)." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore imprevisto durante l'ottimizzazione del percorso.");
                return StatusCode(500, new { error = "Si è verificato un errore imprevisto durante l'ottimizzazione." });
            }
        }

        private string NormalizeString(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            // Semplice normalizzazione: lowercase e rimozione spazi extra. Potrebbe essere più robusta.
            return new string(input.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        }
    }
}

