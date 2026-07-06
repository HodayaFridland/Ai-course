using Microsoft.AspNetCore.Mvc;
using ProductCatalogService.Models;
using ProductCatalogService.Repositories;

namespace ProductCatalogService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _repository;

    public CategoriesController(ICategoryRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Category>>> GetAll() =>
        Ok(await _repository.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<Category>> GetById(string id)
    {
        var category = await _repository.GetByIdAsync(id);
        return category == null
            ? NotFound(new { message = $"Category {id} not found." })
            : Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<Category>> Create([FromBody] Category category)
    {
        var created = await _repository.CreateAsync(category);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
