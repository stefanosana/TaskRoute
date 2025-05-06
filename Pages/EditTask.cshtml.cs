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
    [Authorize]
    public class EditTaskModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public EditTaskModel(ApplicationDbContext context) => _context = context;

        [BindProperty]
        public Commission Commission { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Commission = await _context.Commissions
                .Include(c => c.Location)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (Commission == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Rimuovi la validazione ModelState per SpecificTime e EstimatedDurationMinutes se sono null
            ModelState.Remove("Commission.SpecificTime");
            ModelState.Remove("Commission.EstimatedDurationMinutes");

            // Se SpecificTime è fornito nel form ma non è valido (es. stringa vuota), 
            // il binding potrebbe impostarlo a TimeSpan.Zero. Lo consideriamo come non fornito.
            if (Commission.SpecificTime == TimeSpan.Zero)
            {
                Commission.SpecificTime = null;
            }

            // if (!ModelState.IsValid)
            // {
            //     // Ricarica i dati di navigazione se necessario prima di ritornare la pagina
            //     // if (Commission.LocationId.HasValue && Commission.Location == null)
            //     // {
            //     //     Commission.Location = await _context.Locations.FindAsync(Commission.LocationId.Value);
            //     // }
            //     return Page();
            // }

            var commissionToUpdate = await _context.Commissions
                .Include(c => c.Location) // Includi la location per poterla eventualmente modificare o dissociare
                .FirstOrDefaultAsync(c => c.Id == Commission.Id);

            if (commissionToUpdate == null)
            {
                return NotFound();
            }

            // Verifica che l'utente che modifica sia il proprietario della commissione
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (commissionToUpdate.UserId != userId)
            {
                return Forbid(); // O un altro risultato appropriato per accesso non autorizzato
            }

            // Aggiorna i campi della commissione
            commissionToUpdate.Title = Commission.Title;
            commissionToUpdate.Description = Commission.Description;
            commissionToUpdate.DueDate = Commission.DueDate;
            commissionToUpdate.SpecificTime = Commission.SpecificTime; // Nuovo campo
            commissionToUpdate.EstimatedDurationMinutes = Commission.EstimatedDurationMinutes; // Nuovo campo
            commissionToUpdate.IsCompleted = Commission.IsCompleted;
            if (Commission.IsCompleted && !commissionToUpdate.CompletedAt.HasValue)
            {
                commissionToUpdate.CompletedAt = DateTime.UtcNow;
            }
            else if (!Commission.IsCompleted)
            {
                commissionToUpdate.CompletedAt = null;
            }

            // Gestione della Location (come in AddTask, ma considera la possibilità di rimuovere una location esistente)
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
                    await _context.SaveChangesAsync();
                }
                commissionToUpdate.LocationId = loc.Id;
                commissionToUpdate.Location = loc; // Assicura che anche la proprietà di navigazione sia aggiornata
            }
            else
            {
                // Se i campi della location non sono forniti o sono incompleti, dissocia la location esistente
                commissionToUpdate.LocationId = null;
                commissionToUpdate.Location = null;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CommissionExists(Commission.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./TaskList");
        }

        private async Task<bool> CommissionExists(int id)
        {
            return await _context.Commissions.AnyAsync(e => e.Id == id);
        }
    }
}

