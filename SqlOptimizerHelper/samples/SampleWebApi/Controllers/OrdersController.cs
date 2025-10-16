using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SampleWebApi.Data;
using SampleWebApi.Models;

namespace SampleWebApi.Controllers
{
    /// <summary>
    /// Controller for order operations
    /// This controller demonstrates N+1 query problems and optimization scenarios
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(AppDbContext context, ILogger<OrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets all orders - demonstrates N+1 query problem
        /// This method intentionally creates an N+1 problem to demonstrate the SqlOptimizerHelper
        /// </summary>
        [HttpGet("n1-problem")]
        public async Task<ActionResult<IEnumerable<object>>> GetOrdersWithN1Problem()
        {
            // This demonstrates the N+1 query problem:
            // 1. First query: SELECT * FROM Orders
            // 2. Then for each order: SELECT * FROM Customers WHERE Id = @customerId
            var orders = await _context.Orders.ToListAsync();
            
            var result = new List<object>();
            foreach (var order in orders)
            {
                // This will trigger N+1 detection as we're querying customers individually
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == order.CustomerId);
                
                result.Add(new
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    Status = order.Status,
                    TotalAmount = order.TotalAmount,
                    OrderDate = order.OrderDate,
                    CustomerName = customer?.FirstName + " " + customer?.LastName,
                    CustomerEmail = customer?.Email
                });
            }
            
            return Ok(result);
        }

        /// <summary>
        /// Gets all orders with optimized query using Include - demonstrates the solution to N+1
        /// </summary>
        [HttpGet("optimized")]
        public async Task<ActionResult<IEnumerable<object>>> GetOrdersOptimized()
        {
            // This is the optimized version that solves the N+1 problem:
            // Single query with JOIN: SELECT * FROM Orders o INNER JOIN Customers c ON o.CustomerId = c.Id
            var orders = await _context.Orders
                .Include(o => o.Customer) // This prevents N+1 by loading customers in one query
                .ToListAsync();
            
            var result = orders.Select(order => new
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                OrderDate = order.OrderDate,
                CustomerName = order.Customer.FirstName + " " + order.Customer.LastName,
                CustomerEmail = order.Customer.Email
            });
            
            return Ok(result);
        }

        /// <summary>
        /// Gets orders by customer ID - demonstrates missing index detection
        /// </summary>
        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByCustomer(int customerId)
        {
            // This query will trigger missing index detection for the CustomerId column
            // SQL: SELECT * FROM Orders WHERE CustomerId = @customerId
            var orders = await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .ToListAsync();
            
            return Ok(orders);
        }

        /// <summary>
        /// Gets orders by status - demonstrates missing index detection
        /// </summary>
        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByStatus(string status)
        {
            // This query will trigger missing index detection for the Status column
            // SQL: SELECT * FROM Orders WHERE Status = @status
            var orders = await _context.Orders
                .Where(o => o.Status == status)
                .ToListAsync();
            
            return Ok(orders);
        }

        /// <summary>
        /// Gets orders by date range - demonstrates range query and missing index detection
        /// </summary>
        [HttpGet("date-range")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            // This query will trigger missing index detection for the OrderDate column
            // SQL: SELECT * FROM Orders WHERE OrderDate BETWEEN @startDate AND @endDate
            var orders = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .ToListAsync();
            
            return Ok(orders);
        }

        /// <summary>
        /// Gets orders with order items - demonstrates complex N+1 problem
        /// </summary>
        [HttpGet("with-items/n1-problem")]
        public async Task<ActionResult<IEnumerable<object>>> GetOrdersWithItemsN1Problem()
        {
            // This demonstrates a more complex N+1 problem:
            // 1. First query: SELECT * FROM Orders
            // 2. For each order: SELECT * FROM OrderItems WHERE OrderId = @orderId
            // 3. For each order item: SELECT * FROM Products WHERE Id = @productId
            var orders = await _context.Orders.ToListAsync();
            
            var result = new List<object>();
            foreach (var order in orders)
            {
                // This will trigger N+1 detection
                var orderItems = await _context.OrderItems
                    .Where(oi => oi.OrderId == order.Id)
                    .ToListAsync();
                
                var itemsWithProducts = new List<object>();
                foreach (var item in orderItems)
                {
                    // This will also trigger N+1 detection
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.Id == item.ProductId);
                    
                    itemsWithProducts.Add(new
                    {
                        item.Id,
                        item.Quantity,
                        item.UnitPrice,
                        item.TotalPrice,
                        ProductName = product?.Name,
                        ProductCategory = product?.Category
                    });
                }
                
                result.Add(new
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    Status = order.Status,
                    TotalAmount = order.TotalAmount,
                    OrderDate = order.OrderDate,
                    Items = itemsWithProducts
                });
            }
            
            return Ok(result);
        }

        /// <summary>
        /// Gets orders with order items - optimized version
        /// </summary>
        [HttpGet("with-items/optimized")]
        public async Task<ActionResult<IEnumerable<object>>> GetOrdersWithItemsOptimized()
        {
            // This is the optimized version that solves the N+1 problem:
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product) // This prevents N+1 by loading related data in one query
                .ToListAsync();
            
            var result = orders.Select(order => new
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                OrderDate = order.OrderDate,
                Items = order.OrderItems.Select(item => new
                {
                    item.Id,
                    item.Quantity,
                    item.UnitPrice,
                    item.TotalPrice,
                    ProductName = item.Product.Name,
                    ProductCategory = item.Product.Category
                })
            });
            
            return Ok(result);
        }

        /// <summary>
        /// Gets a specific order by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            
            if (order == null)
            {
                return NotFound();
            }
            
            return Ok(order);
        }

        /// <summary>
        /// Creates a new order
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }

        /// <summary>
        /// Updates an existing order
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, Order order)
        {
            if (id != order.Id)
            {
                return BadRequest();
            }
            
            _context.Entry(order).State = EntityState.Modified;
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
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
        /// Deletes an order
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}
