using AccessControl.Application.DTOs;
using AccessControl.Application.Services;
using AccessControl.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/access-events")]
[Authorize(Roles = AppRoles.AdminPrivada + "," + AppRoles.Guardia + "," + AppRoles.SuperAdmin)]
public class AccessEventsController : ControllerBase
{
    private readonly IAccessEventService _service;

    public AccessEventsController(IAccessEventService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<AccessEventDto>>> GetRecent([FromQuery] int limit = 50) => Ok(await _service.GetRecentAsync(limit));
}
