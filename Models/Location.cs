using System.ComponentModel.DataAnnotations;

namespace TaskRoute.Models
{
    public class Location
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } // Nome del luogo (es. "Supermercato")

        [StringLength(200)]
        public string Address { get; set; } // Indirizzo completo

        [StringLength(50)]
        public string City { get; set; } // Città

        [StringLength(20)]
        public string PostalCode { get; set; } // Codice postale

        [StringLength(50)]
        public string Country { get; set; } // Paese

        public double Latitude { get; set; } // Latitudine (per geolocalizzazione)

        public double Longitude { get; set; } // Longitudine (per geolocalizzazione)
    }
}
