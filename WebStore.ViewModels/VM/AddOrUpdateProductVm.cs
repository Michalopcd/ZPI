using System.ComponentModel.DataAnnotations;

namespace WebStore.ViewModels.VM
{
    public class AddOrUpdateProductVm
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Opis produktu jest wymagany.")]
        public string Description { get; set; } = default!;

        [Required(ErrorMessage = "Zdjęcie produktu jest wymagane.")]
        public byte[] ImageBytes { get; set; } = default!;

        [Required(ErrorMessage = "Nazwa produktu jest wymagana.")]
        public string Name { get; set; } = default!;

        [Required(ErrorMessage = "Cena produktu jest wymagana.")]
        public decimal? Price { get; set; }

        [Required(ErrorMessage = "Waga produktu jest wymagana.")]
        public float? Weight { get; set; }

        [Required(ErrorMessage = "Kategoria produktu jest wymagana.")]
        public int? CategoryId { get; set; }

        [Required(ErrorMessage = "Dostawca produktu jest wymagany.")]
        public int? SupplierId { get; set; }
    }
}
