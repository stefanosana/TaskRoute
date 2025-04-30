using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskRoute.Data;
using TaskRoute.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace TaskRoute.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        // Proprietà per passare i dati alla vista
        public List<TaskRoute.Models.Commission> Tasks { get; set; }
        public int CompletedTasksCount { get; set; }
        public int OverdueTasksCount { get; set; }

        public DashboardModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async System.Threading.Tasks.Task OnGetAsync()
        {
            // Ottieni l'utente corrente
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return;
            }

            // Recupera i task dell'utente corrente
            Tasks = await _context.Commissions
                .Include(t => t.Location) // Include la relazione con Location
                .Where(t => t.UserId == user.Id)
                .ToListAsync();

            // Calcola le statistiche
            CompletedTasksCount = Tasks.Count(t => t.IsCompleted);
            OverdueTasksCount = Tasks.Count(t => !t.IsCompleted && t.DueDate < DateTime.Now);
        }
    }
}