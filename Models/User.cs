namespace WebApiApp.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage ="Name is required")]
        [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Enail is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

    }
}
