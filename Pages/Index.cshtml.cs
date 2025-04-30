using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaskRoute.Data;
using TaskRoute.Models;

namespace TaskRoute.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public IndexModel(ApplicationDbContext context) => _context = context;

        // Solo le commissioni ancora non completate
        public List<Commission> Commissions { get; set; }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Commissions = await _context.Commissions
                .Include(c => c.Location)
                .Where(c => c.UserId == userId && !c.IsCompleted)
                .ToListAsync();
        }
    }
}
