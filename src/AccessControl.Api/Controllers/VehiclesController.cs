using AccessControl.Application.DTOs;
using AccessControl.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _service;

    public VehiclesController(IVehicleService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<VehicleDto>>> GetAll([FromQuery] Guid? houseId) => Ok(await _service.GetAllAsync(houseId));

    [HttpPost]
    public async Task<ActionResult<VehicleDto>> Create(VehicleCreateDto dto) => Ok(await _service.CreateAsync(dto));
}
