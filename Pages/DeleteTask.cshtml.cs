using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaskRoute.Data;
using TaskRoute.Models;
using MyTask = TaskRoute.Models.Commission;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace TaskRoute.Pages
{
    [Authorize]
    public class DeleteTaskModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        [BindProperty]
        public MyTask Task { get; set; }

        public DeleteTaskModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Metodo GET per caricare il task da eliminare
        public async Task<IActionResult> OnGetAsync(int id)
        {
            Task = await _context.Commissions.FindAsync(id);

            if (Task == null)
            {
                return NotFound();
            }

            return Page();
        }

        // Metodo POST per eliminare il task
        public async Task<IActionResult> OnPostAsync(int id)
        {
            var taskToDelete = await _context.Commissions.FindAsync(id);

            if (taskToDelete != null)
            {
                _context.Commissions.Remove(taskToDelete);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./TaskList");
        }
    }
}