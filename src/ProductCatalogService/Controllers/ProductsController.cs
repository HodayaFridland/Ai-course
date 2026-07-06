using Microsoft.AspNetCore.Mvc;
using ProductCatalogService.DTOs;
using ProductCatalogService.Models;
using ProductCatalogService.Repositories;

namespace ProductCatalogService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductRepository repository, ILogger<ProductsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // GET /api/products  — browse the whole catalog
    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetAll()
    {
        var products = await _repository.GetAllAsync();

        // Return the container name in a header so we can PROVE load balancing in Phase 3
        // (each replica has a different machine/host name).
        Response.Headers["X-Instance-Id"] = Environment.MachineName;

        return Ok(products);
    }

    // GET /api/products/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(string id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
            return NotFound(new { message = $"Product {id} not found." });

        Response.Headers["X-Instance-Id"] = Environment.MachineName;
        return Ok(product);
    }

    // GET /api/products/category/{categoryId}
    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<List<Product>>> GetByCategory(string categoryId)
    {
        var products = await _repository.GetByCategoryAsync(categoryId);
        return Ok(products);
    }

    // POST /api/products  — add a product to the catalog
    [HttpPost]
    public async Task<ActionResult<Product>> Create([FromBody] ProductCreateDto dto)
    {
        var product = new Product
        {
            Id = dto.Id ?? string.Empty,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            CategoryId = dto.CategoryId,
            CategoryName = dto.CategoryName,
            Attributes = dto.Attributes
        };

        var created = await _repository.CreateAsync(product);
        _logger.LogInformation("Product created: {ProductId} {Name}", created.Id, created.Name);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT /api/products/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(string id, [FromBody] ProductCreateDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            CategoryId = dto.CategoryId,
            CategoryName = dto.CategoryName,
            Attributes = dto.Attributes
        };

        var updated = await _repository.UpdateAsync(id, product);
        if (!updated)
            return NotFound(new { message = $"Product {id} not found." });

        _logger.LogInformation("Product updated: {ProductId}", id);
        return NoContent();
    }

    // DELETE /api/products/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = $"Product {id} not found." });

        return NoContent();
    }
}
