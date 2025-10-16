using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SampleWebApi.Data;
using SampleWebApi.Models;

namespace SampleWebApi.Controllers
{
    /// <summary>
    /// Controller for customer operations
    /// This controller demonstrates various SQL optimization scenarios
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(AppDbContext context, ILogger<CustomersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets all customers - demonstrates basic query
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            // This query will be fast as it's a simple SELECT * FROM Customers
            var customers = await _context.Customers.ToListAsync();
            return Ok(customers);
        }

        /// <summary>
        /// Gets customer by email - demonstrates missing index detection
        /// </summary>
        [HttpGet("email/{email}")]
        public async Task<ActionResult<Customer>> GetCustomerByEmail(string email)
        {
            // This query will trigger missing index detection for the Email column
            // SQL: SELECT * FROM Customers WHERE Email = @email
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == email);
            
            if (customer == null)
            {
                return NotFound();
            }
            
            return Ok(customer);
        }

        /// <summary>
        /// Gets customers by city - demonstrates missing index detection
        /// </summary>
        [HttpGet("city/{city}")]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomersByCity(string city)
        {
            // This query will trigger missing index detection for the City column
            // SQL: SELECT * FROM Customers WHERE City = @city
            var customers = await _context.Customers
                .Where(c => c.City == city)
                .ToListAsync();
            
            return Ok(customers);
        }

        /// <summary>
        /// Gets customers by country - demonstrates missing index detection
        /// </summary>
        [HttpGet("country/{country}")]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomersByCountry(string country)
        {
            // This query will trigger missing index detection for the Country column
            // SQL: SELECT * FROM Customers WHERE Country = @country
            var customers = await _context.Customers
                .Where(c => c.Country == country)
                .ToListAsync();
            
            return Ok(customers);
        }

        /// <summary>
        /// Searches customers by name - demonstrates LIKE query and missing index detection
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Customer>>> SearchCustomers([FromQuery] string name)
        {
            // This query will trigger missing index detection for the FirstName and LastName columns
            // SQL: SELECT * FROM Customers WHERE FirstName LIKE '%name%' OR LastName LIKE '%name%'
            var customers = await _context.Customers
                .Where(c => c.FirstName.Contains(name) || c.LastName.Contains(name))
                .ToListAsync();
            
            return Ok(customers);
        }

        /// <summary>
        /// Gets customers with their orders - demonstrates N+1 query problem
        /// </summary>
        [HttpGet("with-orders/n1-problem")]
        public async Task<ActionResult<IEnumerable<object>>> GetCustomersWithOrdersN1Problem()
        {
            // This demonstrates the N+1 query problem:
            // 1. First query: SELECT * FROM Customers
            // 2. Then for each customer: SELECT * FROM Orders WHERE CustomerId = @customerId
            var customers = await _context.Customers.ToListAsync();
            
            var result = new List<object>();
            foreach (var customer in customers)
            {
                // This will trigger N+1 detection as we're querying orders individually
                var orders = await _context.Orders
                    .Where(o => o.CustomerId == customer.Id)
                    .ToListAsync();
                
                result.Add(new
                {
                    CustomerId = customer.Id,
                    CustomerName = customer.FirstName + " " + customer.LastName,
                    Email = customer.Email,
                    City = customer.City,
                    OrderCount = orders.Count,
                    TotalSpent = orders.Sum(o => o.TotalAmount),
                    Orders = orders.Select(o => new
                    {
                        o.Id,
                        o.OrderNumber,
                        o.Status,
                        o.TotalAmount,
                        o.OrderDate
                    })
                });
            }
            
            return Ok(result);
        }

        /// <summary>
        /// Gets customers with their orders - optimized version
        /// </summary>
        [HttpGet("with-orders/optimized")]
        public async Task<ActionResult<IEnumerable<object>>> GetCustomersWithOrdersOptimized()
        {
            // This is the optimized version that solves the N+1 problem:
            // Single query with JOIN: SELECT * FROM Customers c LEFT JOIN Orders o ON c.Id = o.CustomerId
            var customers = await _context.Customers
                .Include(c => c.Orders) // This prevents N+1 by loading orders in one query
                .ToListAsync();
            
            var result = customers.Select(customer => new
            {
                CustomerId = customer.Id,
                CustomerName = customer.FirstName + " " + customer.LastName,
                Email = customer.Email,
                City = customer.City,
                OrderCount = customer.Orders.Count,
                TotalSpent = customer.Orders.Sum(o => o.TotalAmount),
                Orders = customer.Orders.Select(o => new
                {
                    o.Id,
                    o.OrderNumber,
                    o.Status,
                    o.TotalAmount,
                    o.OrderDate
                })
            });
            
            return Ok(result);
        }

        /// <summary>
        /// Gets a specific customer by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            
            if (customer == null)
            {
                return NotFound();
            }
            
            return Ok(customer);
        }

        /// <summary>
        /// Creates a new customer
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Customer>> CreateCustomer(Customer customer)
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
        }

        /// <summary>
        /// Updates an existing customer
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, Customer customer)
        {
            if (id != customer.Id)
            {
                return BadRequest();
            }
            
            _context.Entry(customer).State = EntityState.Modified;
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            
            return NoContent();
        }

        /// <summary>
        /// Deletes a customer
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.Id == id);
        }
    }
}
