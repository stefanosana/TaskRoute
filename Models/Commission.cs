using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TaskRoute.Models
{
    public class Commission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } // Titolo della commissione

        [StringLength(500)]
        public string Description { get; set; } // Descrizione dettagliata

        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; } // Data di scadenza

        public bool IsCompleted { get; set; } = false; // Stato di completamento

        [ForeignKey("Location")]
        public int? LocationId { get; set; } // ID della posizione associata (opzionale)

        public Location Location { get; set; } // Relazione con la posizione geografica

        // Proprietà per associare il task all'utente
        [Required]
        public string UserId { get; set; } // ID dell'utente proprietario

        // Relazione con l'utente
        public IdentityUser User { get; set; }

    }
}
