using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Logging.Data;
using Logging.Models;
using Serilog;

namespace Logging.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(ApplicationDbContext context, ILogger<EmployeesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Employees
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
        {
            try
            {
                _logger.LogInformation("Retrieving all employees");
                var employees = await _context.Employees.ToListAsync();
                _logger.LogInformation("Successfully retrieved {EmployeeCount} employees", employees.Count);
                return employees;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving employees");
                return StatusCode(500, "An error occurred while retrieving employees");
            }
        }

        // GET: api/Employees/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployee(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving employee with ID: {EmployeeId}", id);
                var employee = await _context.Employees.FindAsync(id);

                if (employee == null)
                {
                    _logger.LogWarning("Employee with ID {EmployeeId} not found", id);
                    return NotFound();
                }

                _logger.LogInformation("Successfully retrieved employee with ID: {EmployeeId}", id);
                return employee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving employee with ID: {EmployeeId}", id);
                return StatusCode(500, $"An error occurred while retrieving employee with ID {id}");
            }
        }

        // POST: api/Employees
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Employee>> CreateEmployee([FromBody] Employee employee)
        {
            // Validate the input
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try 
            {
                // Explicitly set ID to 0 to ensure database generates it
                employee.Id = 0;

                // Add the new employee to the context
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                // Log the creation of a new employee
                _logger.LogInformation("Created new employee: {EmployeeName} with ID {EmployeeId}", 
                    employee.Name, employee.Id);

                // Return a 201 Created response with the created employee
                return CreatedAtAction(
                    nameof(GetEmployee), 
                    new { id = employee.Id }, 
                    employee
                );
            }
            catch (Exception ex)
            {
                // Log any unexpected errors
                _logger.LogError(ex, "Error creating employee: {ErrorMessage}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "An error occurred while creating the employee." });
            }
        }

        // PUT: api/Employees/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, Employee employee)
        {
            try
            {
                _logger.LogInformation("Updating employee with ID: {EmployeeId}", id);
                
                if (id != employee.Id)
                {
                    _logger.LogWarning("Employee ID mismatch. Requested ID: {RequestedId}, Employee ID: {EmployeeId}", id, employee.Id);
                    return BadRequest();
                }

                _context.Entry(employee).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!EmployeeExists(id))
                    {
                        _logger.LogWarning("Employee with ID {EmployeeId} not found during update", id);
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError(ex, "Concurrency error occurred while updating employee with ID: {EmployeeId}", id);
                        throw;
                    }
                }

                _logger.LogInformation("Successfully updated employee with ID: {EmployeeId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating employee with ID: {EmployeeId}", id);
                return StatusCode(500, $"An error occurred while updating employee with ID {id}");
            }
        }

        // DELETE: api/Employees/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            try
            {
                _logger.LogInformation("Deleting employee with ID: {EmployeeId}", id);
                
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    _logger.LogWarning("Employee with ID {EmployeeId} not found for deletion", id);
                    return NotFound();
                }

                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted employee with ID: {EmployeeId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting employee with ID: {EmployeeId}", id);
                return StatusCode(500, $"An error occurred while deleting employee with ID {id}");
            }
        }

        // GET: api/Employees/average-salary
        [HttpGet("average-salary")]
        public ActionResult<decimal> GetAverageSalary()
        {
            try
            {
                _logger.LogInformation("Calculating average salary for all employees");

                var employees = _context.Employees.ToList();

                // Potential divide by zero scenario
                if (employees.Count == 0)
                {
                    _logger.LogWarning("No employees found");
                    return NotFound("No employees found");
                }

                decimal totalSalary = employees.Sum(e => e.Salary ?? 0);
                decimal averageSalary = employees.Count > 0 
                    ? totalSalary / employees.Count 
                    : 0;

                _logger.LogInformation(
                    "Average salary calculated: {AverageSalary} for {EmployeeCount} employees", 
                    averageSalary, 
                    employees.Count
                );

                return averageSalary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calculating average salary");
                return StatusCode(500, $"Error calculating average salary: {ex.Message}");
            }
        }

        // Demonstration of different exception types
        [HttpGet("throw-unauthorized")]
        public IActionResult ThrowUnauthorizedException()
        {
            throw new UnauthorizedAccessException("You do not have permission to access this resource.");
        }

        [HttpGet("throw-argument")]
        public IActionResult ThrowArgumentException(int value)
        {
            if (value < 0)
            {
                throw new ArgumentException("Value must be non-negative.", nameof(value));
            }
            return Ok($"Value received: {value}");
        }

        [HttpGet("throw-not-found")]
        public IActionResult ThrowNotFoundException(int employeeId)
        {
            // Simulating a scenario where an employee is not found
            throw new KeyNotFoundException($"Employee with ID {employeeId} was not found.");
        }

        [HttpGet("throw-database")]
        public async Task<IActionResult> ThrowDatabaseException()
        {
            // Simulating a database update conflict
            try 
            {
                // This is a contrived example to simulate a database exception
                var employee = new Employee 
                { 
                    Id = -1, // Invalid ID to trigger an error
                    Name = "Test Employee" 
                };
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                throw new DbUpdateException("Error updating database", ex);
            }
        }

        [HttpGet("throw-unexpected")]
        public IActionResult ThrowUnexpectedException()
        {
            // Simulating an unexpected exception
            throw new InvalidOperationException("An unexpected error occurred in the system.");
        }

        [HttpGet("calculator")]
        public IActionResult Calculator(int numerator, int denominator)
        {
            try 
            {
                int result = numerator / denominator;
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while dividing {Numerator} by {Denominator}", numerator, denominator);
                return StatusCode(400, "Cannot divide by zero");
            }
        }

        [HttpPost("seed-employees")]
        public async Task<IActionResult> SeedEmployees()
        {
            // Check if employees already exist
            if (_context.Employees.Any())
            {
                return BadRequest("Employees already exist in the database.");
            }

            // Create sample employees
            var employees = new List<Employee>
            {
                new Employee 
                { 
                    Name = "John Doe", 
                    Email = "john.doe@company.com", 
                    Salary = 75000, 
                    JoinDate = DateTime.Now.AddYears(-3)
                },
                new Employee 
                { 
                    Name = "Jane Smith", 
                    Email = "jane.smith@company.com", 
                    Salary = 85000, 
                    JoinDate = DateTime.Now.AddYears(-2)
                },
                new Employee 
                { 
                    Name = "Mike Johnson", 
                    Email = "mike.johnson@company.com", 
                    Salary = 65000, 
                    JoinDate = DateTime.Now.AddYears(-1)
                },
                new Employee 
                { 
                    Name = "Emily Brown", 
                    Email = "emily.brown@company.com", 
                    Salary = 95000, 
                    JoinDate = DateTime.Now.AddYears(-4)
                }
            };

            // Add employees to context and save
            _context.Employees.AddRange(employees);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Seeded {EmployeeCount} employees", employees.Count);

            return Ok(new 
            { 
                Message = "Successfully seeded employees", 
                EmployeeCount = employees.Count
            });
        }

        // GET: api/Employees/calculator
        

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.Id == id);
        }
    }
}
