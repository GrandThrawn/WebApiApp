namespace WebApiApp.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column(TypeName = "uuid")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage ="Name is required")]
        [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = false;
        public bool IsAdmin { get; set; } = false;
        public bool IsVerified { get; set; } = false;
        public string? MailValidationToken { get; set; }
        public bool IsEnable2fa { get; set; } = false;
        public string? OtpValidationToken {  get; set; }
        public string? OtpSecret {  get; set; }


    }
}

