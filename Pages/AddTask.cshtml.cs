using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskRoute.Data;
using TaskRoute.Models;

namespace TaskRoute.Pages
{
    public class AddTaskModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public AddTaskModel(ApplicationDbContext ctx) => _context = ctx;

        [BindProperty]
        public Commission Commission { get; set; }

        // NON bindiamo la Location come proprietà complessa, usiamo Request.Form

        public void OnGet()
        {
            // niente da fare qui
        }

        public async Task<IActionResult> OnPostAsync()
        {
            //if (!ModelState.IsValid)
            //    return Page();

            // Se ho dei dati di posizione:
            var latStr = Request.Form["Latitude"];
            if (!string.IsNullOrEmpty(latStr))
            {
                var loc = new Location
                {
                    Name = Request.Form["LocationName"],
                    Address = Request.Form["LocationAddress"],
                    City = Request.Form["City"],
                    PostalCode = Request.Form["PostalCode"],
                    Country = Request.Form["Country"],
                    Latitude = double.Parse(latStr),
                    Longitude = double.Parse(Request.Form["Longitude"])
                };
                _context.Locations.Add(loc);
                await _context.SaveChangesAsync();

                Commission.LocationId = loc.Id;
            }

            // Imposto l'utente corrente
            Commission.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            _context.Commissions.Add(Commission);
            await _context.SaveChangesAsync();

            return RedirectToPage("./TaskList");
        }
    }
}
