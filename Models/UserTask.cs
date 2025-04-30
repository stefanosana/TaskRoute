using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TaskRoute.Models
{
    public class UserTask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // ID dell'utente (da Identity)

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; } // Relazione con l'utente

        [Required]
        public int TaskId { get; set; } // ID della commissione

        [ForeignKey("TaskId")]
        public Commission Task { get; set; } // Relazione con la commissione

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow; // Data di assegnazione

        public bool IsAccepted { get; set; } = false; // Se l'utente ha accettato la commissione
    }
}
