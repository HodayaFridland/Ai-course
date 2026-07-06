using InventoryService.DTOs;
using InventoryService.Models;
using InventoryService.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IStockService _stock;

    public InventoryController(IStockService stock)
    {
        _stock = stock;
    }

    // GET /api/inventory  — see all stock levels
    [HttpGet]
    public async Task<ActionResult<List<InventoryItem>>> GetAll() =>
        Ok(await _stock.GetAllAsync());

    // GET /api/inventory/{productId}
    [HttpGet("{productId}")]
    public async Task<ActionResult<InventoryItem>> Get(string productId)
    {
        var item = await _stock.GetAsync(productId);
        return item == null
            ? NotFound(new { message = $"No stock record for product '{productId}'." })
            : Ok(item);
    }

    // POST /api/inventory  — set/replace the stock quantity for a product
    [HttpPost]
    public async Task<ActionResult<InventoryItem>> SetStock([FromBody] SetStockDto dto) =>
        Ok(await _stock.SetStockAsync(dto));

    // POST /api/inventory/reserve  — reserve stock for an order (all-or-nothing)
    [HttpPost("reserve")]
    public async Task<ActionResult<ReserveResult>> Reserve([FromBody] ReserveRequest request)
    {
        var result = await _stock.ReserveAsync(request);
        // 200 with Success=false is fine here — the caller decides what to do with a rejection.
        return Ok(result);
    }

    // POST /api/inventory/release  — give stock back (compensation)
    [HttpPost("release")]
    public async Task<ActionResult> Release([FromBody] ReserveRequest request)
    {
        await _stock.ReleaseAsync(request);
        return NoContent();
    }
}
