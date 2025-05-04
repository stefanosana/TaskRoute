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
    [Authorize] // Solo gli utenti autenticati possono accedere a questa pagina
    public class AddTaskModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public AddTaskModel(ApplicationDbContext ctx) => _context = ctx;

        [BindProperty]
        public Commission Commission { get; set; }

        // NON bindiamo la Location come proprietà complessa, usiamo Request.Form

        public void OnGet()
        {
            // inizializza eventualmente valori di default
        }

        public async Task<IActionResult> OnPostAsync()
        {
            //if (!ModelState.IsValid)
            //    return Page();

            // Leggo i dati di Location dal form
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

                // 1) cerco una Location con gli stessi dati (puoi adattare i criteri)
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

                // 2) se non esiste, la creo
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
                    await _context.SaveChangesAsync(); // per ottenere loc.Id
                }

                // 3) assegno l'Id di Location alla Commission
                Commission.LocationId = loc.Id;
            }

            // Imposto l'utente corrente
            Commission.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 4) salvo la Commission
            _context.Commissions.Add(Commission);
            await _context.SaveChangesAsync();

            return RedirectToPage("./TaskList");
        }
    }
}
