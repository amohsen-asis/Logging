using System;
using System.ComponentModel.DataAnnotations;

namespace Logging.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [DataType(DataType.Date)]
        public DateTime? JoinDate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Salary { get; set; }
    }
}
