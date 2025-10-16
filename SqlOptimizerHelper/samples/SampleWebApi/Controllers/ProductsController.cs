using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SampleWebApi.Data;
using SampleWebApi.Models;

namespace SampleWebApi.Controllers
{
    /// <summary>
    /// Controller for product operations
    /// This controller demonstrates various SQL optimization scenarios
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(AppDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets all products - demonstrates basic query
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            // This query will be fast as it's a simple SELECT * FROM Products
            var products = await _context.Products.ToListAsync();
            return Ok(products);
        }

        /// <summary>
        /// Gets products by category - demonstrates missing index detection
        /// </summary>
        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(string category)
        {
            // This query will trigger missing index detection for the Category column
            // SQL: SELECT * FROM Products WHERE Category = @category
            var products = await _context.Products
                .Where(p => p.Category == category)
                .ToListAsync();
            
            return Ok(products);
        }

        /// <summary>
        /// Test endpoint to check if interceptor works with sync queries
        /// </summary>
        [HttpGet("test-sync")]
        public ActionResult<IEnumerable<Product>> GetProductsSync()
        {
            Console.WriteLine("[SQL Optimizer] 🔥 Controller: About to execute sync query...");
            
            // This query will be synchronous to test if the interceptor works
            var products = _context.Products
                .Where(p => p.Category == "Electronics")
                .ToList();
            
            Console.WriteLine($"[SQL Optimizer] 🔥 Controller: Query completed, found {products.Count} products");
            
            return Ok(products);
        }

        /// <summary>
        /// Searches products by name - demonstrates LIKE query and missing index detection
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Product>>> SearchProducts([FromQuery] string name)
        {
            // This query will trigger missing index detection for the Name column
            // SQL: SELECT * FROM Products WHERE Name LIKE '%name%'
            var products = await _context.Products
                .Where(p => p.Name.Contains(name))
                .ToListAsync();
            
            return Ok(products);
        }

        /// <summary>
        /// Gets products by price range - demonstrates range query and missing index detection
        /// </summary>
        [HttpGet("price-range")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByPriceRange([FromQuery] decimal minPrice, [FromQuery] decimal maxPrice)
        {
            // This query will trigger missing index detection for the Price column
            // SQL: SELECT * FROM Products WHERE Price BETWEEN @minPrice AND @maxPrice
            var products = await _context.Products
                .Where(p => p.Price >= minPrice && p.Price <= maxPrice)
                .ToListAsync();
            
            return Ok(products);
        }

        /// <summary>
        /// Gets products with low stock - demonstrates complex WHERE clause
        /// </summary>
        [HttpGet("low-stock")]
        public async Task<ActionResult<IEnumerable<Product>>> GetLowStockProducts([FromQuery] int threshold = 30)
        {
            // This query will trigger missing index detection for the StockQuantity column
            // SQL: SELECT * FROM Products WHERE StockQuantity < @threshold AND IsActive = 1
            var products = await _context.Products
                .Where(p => p.StockQuantity < threshold && p.IsActive)
                .ToListAsync();
            
            return Ok(products);
        }

        /// <summary>
        /// Gets a specific product by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            
            if (product == null)
            {
                return NotFound();
            }
            
            return Ok(product);
        }

        /// <summary>
        /// Creates a new product
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        /// <summary>
        /// Updates an existing product
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }
            
            _context.Entry(product).State = EntityState.Modified;
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
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
        /// Deletes a product
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
