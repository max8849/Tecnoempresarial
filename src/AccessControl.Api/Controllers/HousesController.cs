using AccessControl.Application.DTOs;
using AccessControl.Application.Services;
using AccessControl.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/houses")]
[Authorize]
public class HousesController : ControllerBase
{
    private readonly IHouseService _service;

    public HousesController(IHouseService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<HouseDto>>> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HouseDto>> GetById(Guid id) => Ok(await _service.GetByIdAsync(id));

    [HttpPost]
    [Authorize(Roles = AppRoles.AdminPrivada + "," + AppRoles.SuperAdmin)]
    public async Task<ActionResult<HouseDto>> Create(HouseCreateDto dto)
    {
        var creado = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id }, creado);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.AdminPrivada + "," + AppRoles.SuperAdmin)]
    public async Task<IActionResult> Update(Guid id, HouseUpdateDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return NoContent();
    }
}
