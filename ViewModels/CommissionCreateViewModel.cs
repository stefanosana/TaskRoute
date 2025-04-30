using System.ComponentModel.DataAnnotations;

namespace TaskRoute.ViewModels
{
    // 1) Definisci un ViewModel per il form di creazione
    public class CommissionCreateViewModel
    {
        // Proprietà Commission
        [Required, StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        // Proprietà Location
        [Required, StringLength(100)]
        public string LocationName { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(50)]
        public string City { get; set; }

        [StringLength(20)]
        public string PostalCode { get; set; }

        [StringLength(50)]
        public string Country { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }
    }

}
