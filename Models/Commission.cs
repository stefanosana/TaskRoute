using System;
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
        public DateTime DueDate { get; set; } // Data di scadenza (giorno)

        // Orario specifico per la commissione (opzionale)
        [DataType(DataType.Time)]
        public TimeSpan? SpecificTime { get; set; }

        // Durata stimata per lo svolgimento della commissione in minuti (opzionale)
        // Ad esempio: 30 (per 30 minuti), 60 (per 1 ora)
        [Range(0, 1440)] // Limita la durata tra 0 minuti e 24 ore (1440 minuti)
        public int? EstimatedDurationMinutes { get; set; }

        public bool IsCompleted { get; set; } = false; // Stato di completamento

        [DataType(DataType.DateTime)]
        public DateTime? CompletedAt { get; set; }

        [ForeignKey("Location")]
        public int? LocationId { get; set; } // ID della posizione associata (opzionale)

        public Location Location { get; set; } // Relazione con la posizione geografica

        // Proprietà per associare il task all'utente
        [Required]
        public string UserId { get; set; } // ID dell'utente proprietario

        // Relazione con l'utente
        public IdentityUser User { get; set; }

        // Combinazione di DueDate e SpecificTime per un DateTime completo, utile per ordinamenti e logica
        [NotMapped] // Questa proprietà non viene mappata al database direttamente
        public DateTime? DueDateTime
        {
            get
            {
                if (SpecificTime.HasValue)
                {
                    return DueDate.Date + SpecificTime.Value;
                }
                return DueDate.Date; // Se non c'è orario, considera l'inizio della giornata
            }
            set
            {
                if (value.HasValue)
                {
                    DueDate = value.Value.Date;
                    SpecificTime = value.Value.TimeOfDay;
                }
            }
        }
    }
}

