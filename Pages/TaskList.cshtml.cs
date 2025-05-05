using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaskRoute.Data;
using TaskRoute.Models;
using MyTask = TaskRoute.Models.Commission;

namespace TaskRoute.Pages
{
    [Authorize]
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

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            Tasks = await _context.Commissions
                .Where(t => t.UserId == user.Id)
                .ToListAsync();
        }

        // diventa handler Razor Pages OnPostToggleCompletedAsync
        public async Task<JsonResult> OnPostToggleCompletedAsync(int id, bool isCompleted)
        {
            var commission = await _context.Commissions.FindAsync(id);
            if (commission == null)
                return new JsonResult(new { error = "NotFound" }) { StatusCode = 404 };

            commission.IsCompleted = isCompleted;
            commission.CompletedAt = isCompleted
                                       ? DateTime.UtcNow
                                       : (DateTime?)null;

            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                commission.IsCompleted,
                commission.CompletedAt
            });
        }
    }
}
