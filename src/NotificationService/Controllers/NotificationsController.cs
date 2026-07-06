using Microsoft.AspNetCore.Mvc;
using NotificationService.DTOs;
using NotificationService.Models;
using NotificationService.Repositories;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationRepository repository, ILogger<NotificationsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // GET /api/notifications  — full history (newest first)
    [HttpGet]
    public async Task<ActionResult<List<Notification>>> GetAll() =>
        Ok(await _repository.GetAllAsync());

    // GET /api/notifications/user/{userId}
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<Notification>>> GetByUser(int userId) =>
        Ok(await _repository.GetByUserAsync(userId));

    // POST /api/notifications  — record & "send" a notification to the customer
    [HttpPost]
    public async Task<ActionResult<Notification>> Create([FromBody] CreateNotificationDto dto)
    {
        var notification = new Notification
        {
            OrderId = dto.OrderId,
            UserId = dto.UserId,
            Status = dto.Status,
            Message = dto.Message
        };

        var created = await _repository.CreateAsync(notification);

        // In a real system this is where we'd send an email/SMS. Here we log it as "sent".
        _logger.LogInformation("NOTIFY user {UserId} about order {OrderId}: [{Status}] {Message}",
            dto.UserId, dto.OrderId, dto.Status, dto.Message);

        return Ok(created);
    }
}
