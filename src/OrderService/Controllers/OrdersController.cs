using Microsoft.AspNetCore.Mvc;
using OrderService.DTOs;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderProcessingService _orders;

    public OrdersController(IOrderProcessingService orders)
    {
        _orders = orders;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderResponseDto>>> GetAll() =>
        Ok(await _orders.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponseDto>> GetById(int id)
    {
        var order = await _orders.GetByIdAsync(id);
        return order == null
            ? NotFound(new { message = $"Order {id} not found." })
            : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> Create([FromBody] OrderCreateDto dto)
    {
        try
        {
            var order = await _orders.CreateOrderAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
