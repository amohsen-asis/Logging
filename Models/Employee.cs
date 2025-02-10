using System;
using System.ComponentModel.DataAnnotations;

namespace Logging.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Department { get; set; }

        [DataType(DataType.Date)]
        public DateTime JoinDate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Salary { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
