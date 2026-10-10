using AccessControl.Application.DTOs;
using AccessControl.Application.Services;
using AccessControl.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/devices")]
[Authorize(Roles = AppRoles.AdminPrivada + "," + AppRoles.SuperAdmin)]
public class DevicesController : ControllerBase
{
    private readonly IDeviceService _service;

    public DevicesController(IDeviceService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<DeviceDto>>> GetAll() => Ok(await _service.GetAllAsync());

    [HttpPost]
    public async Task<ActionResult<DeviceDto>> Create(DeviceCreateDto dto) => Ok(await _service.CreateAsync(dto));

    [HttpPost("{id:guid}/access-points")]
    public async Task<IActionResult> LinkAccessPoint(Guid id, DeviceLinkAccessPointDto dto)
    {
        await _service.LinkAccessPointAsync(id, dto);
        return NoContent();
    }

    [HttpPost("{id:guid}/test-connection")]
    public async Task<ActionResult<DeviceTestConnectionResultDto>> TestConnection(Guid id) =>
        Ok(await _service.TestConnectionAsync(id));
}
