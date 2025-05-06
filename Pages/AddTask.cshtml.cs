using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaskRoute.Data;
using TaskRoute.Models;
using System;

namespace TaskRoute.Pages
{
    [Authorize] // Solo gli utenti autenticati possono accedere a questa pagina
    public class AddTaskModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public AddTaskModel(ApplicationDbContext ctx) => _context = ctx;

        [BindProperty]
        public Commission Commission { get; set; }

        public void OnGet()
        {
            // Inizializza eventualmente valori di default
            Commission = new Commission
            {
                DueDate = DateTime.Today // Preimposta la data a oggi
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Rimuovi la validazione ModelState per SpecificTime e EstimatedDurationMinutes se sono null
            // in quanto sono opzionali. La validazione del Range per EstimatedDurationMinutes 
            // si attiverà solo se un valore è fornito.
            ModelState.Remove("Commission.SpecificTime");
            ModelState.Remove("Commission.EstimatedDurationMinutes");

            // Se SpecificTime è fornito nel form ma non è valido (es. stringa vuota), 
            // il binding potrebbe impostarlo a TimeSpan.Zero. Lo consideriamo come non fornito.
            if (Commission.SpecificTime == TimeSpan.Zero)
            {
                Commission.SpecificTime = null;
            }

            // Se EstimatedDurationMinutes è fornito come 0, potrebbe essere l'input di default o un errore.
            // Se 0 non è una durata valida o se vuoi trattarlo come non specificato, gestiscilo qui.
            // Per ora, se è 0 e il campo è opzionale, lo accettiamo o lo impostiamo a null se preferito.
            // if (Commission.EstimatedDurationMinutes == 0) Commission.EstimatedDurationMinutes = null;

            // Ricontrolla ModelState dopo aver potenzialmente modificato i valori
            // if (!ModelState.IsValid)
            // {
            //     return Page();
            // }

            // Leggo i dati di Location dal form
            var name = Request.Form["LocationName"].ToString().Trim();
            var address = Request.Form["LocationAddress"].ToString().Trim();
            var city = Request.Form["City"].ToString().Trim();
            var postal = Request.Form["PostalCode"].ToString().Trim();
            var country = Request.Form["Country"].ToString().Trim();
            var latStr = Request.Form["Latitude"].ToString();
            var lonStr = Request.Form["Longitude"].ToString();

            if (!string.IsNullOrEmpty(latStr) && !string.IsNullOrEmpty(lonStr) && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(address))
            {
                var lat = double.Parse(latStr, CultureInfo.InvariantCulture);
                var lon = double.Parse(lonStr, CultureInfo.InvariantCulture);

                var loc = await _context.Locations
                    .FirstOrDefaultAsync(l =>
                        l.Name == name &&
                        l.Address == address &&
                        l.City == city &&
                        l.PostalCode == postal &&
                        l.Country == country &&
                        Math.Abs(l.Latitude - lat) < 0.00001 && // Confronto per double
                        Math.Abs(l.Longitude - lon) < 0.00001  // Confronto per double
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
                    await _context.SaveChangesAsync();
                }
                Commission.LocationId = loc.Id;
            }
            else
            {
                // Se non ci sono dati di location validi, imposta LocationId a null
                Commission.LocationId = null;
            }

            Commission.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            _context.Commissions.Add(Commission);
            await _context.SaveChangesAsync();

            return RedirectToPage("./TaskList");
        }
    }
}

