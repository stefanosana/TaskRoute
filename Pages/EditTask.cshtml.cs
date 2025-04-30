using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaskRoute.Data;
using TaskRoute.Models;
using MyTask = TaskRoute.Models.Commission;


namespace TaskRoute.Pages
{
    [Authorize]
    public class EditTaskModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        [BindProperty]
        public MyTask Task { get; set; }

        public EditTaskModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Metodo GET per caricare il task da modificare
        public async Task<IActionResult> OnGetAsync(int id)
        {
            Task = await _context.Commissions.FindAsync(id);

            if (Task == null)
            {
                return NotFound();
            }

            return Page();
        }

        // Metodo POST per salvare le modifiche
        public async System.Threading.Tasks.Task<IActionResult> OnPostAsync()
        {
            _context.Attach(Task).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskExists(Task.Id))
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

        private bool TaskExists(int id)
        {
            return _context.Commissions.Any(e => e.Id == id);
        }
    }
}