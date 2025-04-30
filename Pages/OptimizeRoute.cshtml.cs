using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskRoute.Services;

namespace TaskRoute.Pages
{
    public class OptimizeRouteModel : PageModel
    {
        private readonly GeminiService _geminiService;

        [BindProperty]
        public string UserInput { get; set; }

        public string AiResponse { get; set; }

        public OptimizeRouteModel(GeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        public async Task OnPostAsync()
        {
            if (!string.IsNullOrWhiteSpace(UserInput))
            {
                AiResponse = await _geminiService.GetResponseAsync(UserInput);
            }
        }
    }
}