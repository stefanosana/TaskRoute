using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaskRoute.Data;
using TaskRoute.Models;

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
            // Carica la commissione (inclusa la Location) soltanto se appartiene all'utente corrente
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Commission = await _context.Commissions
                .Include(c => c.Location)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            if (Commission == null)
                return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Recupera l'entità da aggiornare
            var commissionToUpdate = await _context.Commissions
                .Include(c => c.Location)
                .FirstOrDefaultAsync(c => c.Id == Commission.Id);
            if (commissionToUpdate == null)
                return NotFound();

            // 1) Aggiorna i campi semplici
            commissionToUpdate.Title = Commission.Title;
            commissionToUpdate.Description = Commission.Description;
            commissionToUpdate.DueDate = Commission.DueDate;
            commissionToUpdate.IsCompleted = Commission.IsCompleted;

            // 2) Gestione Location come in AddTask
            var name = Request.Form["LocationName"].ToString().Trim();
            var address = Request.Form["LocationAddress"].ToString().Trim();
            var city = Request.Form["City"].ToString().Trim();
            var postal = Request.Form["PostalCode"].ToString().Trim();
            var country = Request.Form["Country"].ToString().Trim();
            var latStr = Request.Form["Latitude"].ToString();
            var lonStr = Request.Form["Longitude"].ToString();

            if (!string.IsNullOrEmpty(latStr) && !string.IsNullOrEmpty(lonStr))
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
                        l.Latitude == lat &&
                        l.Longitude == lon
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
                    await _context.SaveChangesAsync(); // ottiene loc.Id
                }

                commissionToUpdate.LocationId = loc.Id;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("./TaskList");
        }
    }
}
