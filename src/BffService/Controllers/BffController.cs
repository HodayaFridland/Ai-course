using BffService.Services;
using Microsoft.AspNetCore.Mvc;

namespace BffService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BffController : ControllerBase
{
    private readonly IOrderDetailsService _orderDetailsService;

    public BffController(IOrderDetailsService orderDetailsService)
    {
        _orderDetailsService = orderDetailsService;
    }

    [HttpGet("orders/{id}/details")]
    public async Task<IActionResult> GetOrderDetails(int id)
    {
        var result = await _orderDetailsService.GetOrderDetailsAsync(id);
        return result is null ? NotFound() : Ok(result);
    }
}
