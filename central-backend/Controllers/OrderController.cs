using CentralBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentralBackend.Controllers;

[Authorize]
[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    [HttpPost]
    public IActionResult CreateOrder([FromBody] OrderPayload order)
    {
        Console.WriteLine($"Received order {order.OrderId} from {order.CustomerEmail}");

        return Ok(new
        {
            success = true,
            order.OrderId
        });
    }
}
