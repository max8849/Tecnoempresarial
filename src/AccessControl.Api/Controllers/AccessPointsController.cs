using AccessControl.Application.DTOs;
using AccessControl.Application.Services;
using AccessControl.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/access-points")]
[Authorize]
public class AccessPointsController : ControllerBase
{
    private readonly IAccessPointService _service;

    public AccessPointsController(IAccessPointService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<AccessPointDto>>> GetAll() => Ok(await _service.GetAllAsync());

    [HttpPost]
    [Authorize(Roles = AppRoles.AdminPrivada + "," + AppRoles.SuperAdmin)]
    public async Task<ActionResult<AccessPointDto>> Create(AccessPointCreateDto dto) => Ok(await _service.CreateAsync(dto));
}
