using AccessControl.Application.DTOs;
using AccessControl.Application.Services;
using AccessControl.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/residents")]
[Authorize(Roles = AppRoles.AdminPrivada + "," + AppRoles.SuperAdmin)]
public class ResidentsController : ControllerBase
{
    private readonly IResidentService _service;

    public ResidentsController(IResidentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<ResidentDto>>> GetAll([FromQuery] Guid? houseId) => Ok(await _service.GetAllAsync(houseId));

    [HttpPost]
    public async Task<ActionResult<ResidentDto>> Create(ResidentCreateDto dto) => Ok(await _service.CreateAsync(dto));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ResidentUpdateDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return NoContent();
    }
}
