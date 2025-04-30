using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskRoute.Data;
using TaskRoute.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using MyTask = TaskRoute.Models.Commission; // Per evitare conflitti di nomi

namespace TaskRoute.Pages
{
    [Authorize] // Solo gli utenti autenticati possono accedere a questa pagina
    public class TaskListModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public IList<MyTask> Tasks { get; set; }

        public TaskListModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
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

            // Carica solo i task dell'utente corrente
            Tasks = await _context.Commissions
                .Where(t => t.UserId == user.Id) // Filtra i task per UserId
                .ToListAsync();
        }
    }
}