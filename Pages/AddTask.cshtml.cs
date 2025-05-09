using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // Necessario per DbUpdateException e AnyAsync
using TaskRoute.Data;
using TaskRoute.Models;
using System;
// using Microsoft.Extensions.Logging; // Opzionale, per il logging

namespace TaskRoute.Pages
{
    [Authorize] // Solo gli utenti autenticati possono accedere a questa pagina
    public class AddTaskModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        // private readonly ILogger<AddTaskModel> _logger; // Opzionale, per il logging

        public AddTaskModel(ApplicationDbContext ctx /*, ILogger<AddTaskModel> logger */) // Includere logger se si usa
        {
            _context = ctx;
            // _logger = logger; // Opzionale
        }

        [BindProperty]
        public Commission Commission { get; set; }

        public void OnGet()
        {
            Commission = new Commission
            {
                DueDate = DateTime.Today
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Commission.SpecificTime");
            ModelState.Remove("Commission.EstimatedDurationMinutes");

            if (Commission.SpecificTime == TimeSpan.Zero)
            {
                Commission.SpecificTime = null;
            }

            var name = Request.Form["LocationName"].ToString().Trim();
            var address = Request.Form["LocationAddress"].ToString().Trim();
            var city = Request.Form["City"].ToString().Trim();
            var postal = Request.Form["PostalCode"].ToString().Trim();
            var country = Request.Form["Country"].ToString().Trim();
            var latStr = Request.Form["Latitude"].ToString();
            var lonStr = Request.Form["Longitude"].ToString();

            if (!string.IsNullOrEmpty(latStr) && !string.IsNullOrEmpty(lonStr) && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(address))
            {
                if (double.TryParse(latStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(lonStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                {
                    var loc = await _context.Locations
                        .FirstOrDefaultAsync(l =>
                            l.Name == name &&
                            l.Address == address &&
                            l.City == city &&
                            l.PostalCode == postal &&
                            l.Country == country &&
                            Math.Abs(l.Latitude - lat) < 0.00001 &&
                            Math.Abs(l.Longitude - lon) < 0.00001
                        );

                    if (loc == null)
                    {
                        loc = new Location
                        {
                            Name = name,
                            Address = address,
                            City = city,
                            PostalCode = postal,
                            Country = country,
                            Latitude = lat,
                            Longitude = lon
                        };
                        _context.Locations.Add(loc);
                        try
                        {
                            await _context.SaveChangesAsync(); // Salva la nuova location
                        }
                        catch (DbUpdateException ex)
                        {
                            // _logger.LogError(ex, "Errore durante il salvataggio della nuova Location.");
                            ModelState.AddModelError(string.Empty, "Errore durante il salvataggio dei dati della località. Riprova.");
                            return Page();
                        }
                    }
                    Commission.LocationId = loc.Id;
                }
                else
                {
                    // Se latitudine o longitudine non sono double validi, non creare/associare location
                    Commission.LocationId = null;
                    ModelState.AddModelError("Commission.LocationId", "I valori di latitudine o longitudine non sono validi.");
                    // Non necessariamente un errore bloccante se la location è opzionale, ma informa l'utente.
                }
            }
            else
            {
                Commission.LocationId = null;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError(string.Empty, "Utente non autenticato o non riconosciuto. Impossibile salvare l'attività.");
                return Page();
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                ModelState.AddModelError(string.Empty, "L'utente specificato non esiste nel sistema. Impossibile salvare l'attività.");
                return Page();
            }

            Commission.UserId = userId;

            // Ricontrolla ModelState prima di aggiungere e salvare, nel caso i controlli sulla Location abbiano aggiunto errori
            

            _context.Commissions.Add(Commission);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.Sqlite.SqliteException sqliteEx && sqliteEx.SqliteErrorCode == 19)
            {
                // _logger.LogError(sqliteEx, "Violazione del vincolo di chiave esterna (SQLite Error 19) durante il salvataggio della Commissione. UserId: {UserId}, LocationId: {LocationId}", Commission.UserId, Commission.LocationId);
                ModelState.AddModelError(string.Empty, "Errore durante il salvataggio: i dati forniti per utente o località non sono validi. Verifica che l'utente sia corretto e che la località, se specificata, sia valida.");
                return Page();
            }
            catch (DbUpdateException ex)
            {
                // _logger.LogError(ex, "Errore DbUpdateException generico durante il salvataggio della Commissione.");
                ModelState.AddModelError(string.Empty, "Si è verificato un errore imprevisto durante il salvataggio. Riprova.");
                return Page();
            }

            return RedirectToPage("./TaskList");
        }
    }
}

