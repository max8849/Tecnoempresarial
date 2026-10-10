using AccessControl.Application.Common.Exceptions;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.DTOs;
using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Application.Services;

public interface ITenantService
{
    Task<List<TenantDto>> GetAllAsync(CancellationToken ct = default);
    Task<TenantDto> CreateAsync(TenantCreateDto dto, CancellationToken ct = default);
    Task UpdateAsync(Guid id, TenantUpdateDto dto, CancellationToken ct = default);
    Task<TenantAgentKeyDto> GetAgentKeyAsync(Guid id, CancellationToken ct = default);
    Task<TenantAgentKeyDto> RegenerateAgentKeyAsync(Guid id, CancellationToken ct = default);
}

// Solo lo usa SUPER_ADMIN: administra las privadas (tenants) del sistema completo.
public class TenantService : ITenantService
{
    private readonly IAppDbContext _db;
    private readonly IAgentNotifier _agentNotifier;

    public TenantService(IAppDbContext db, IAgentNotifier agentNotifier)
    {
        _db = db;
        _agentNotifier = agentNotifier;
    }

    public async Task<List<TenantDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Tenants
            .OrderBy(t => t.Name)
            .Select(t => new TenantDto(t.Id, t.Name, t.Slug, t.IsActive, t.Houses.Count))
            .ToListAsync(ct);
    }

    public async Task<TenantDto> CreateAsync(TenantCreateDto dto, CancellationToken ct = default)
    {
        if (await _db.Tenants.AnyAsync(t => t.Slug == dto.Slug, ct))
            throw new ValidationAppException($"Ya existe un tenant con el slug '{dto.Slug}'.");

        var tenant = new Tenant { Name = dto.Name, Slug = dto.Slug };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);
        return new TenantDto(tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive, 0);
    }

    public async Task UpdateAsync(Guid id, TenantUpdateDto dto, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FindAsync(new object?[] { id }, ct) ?? throw new NotFoundException(nameof(Tenant), id);
        tenant.Name = dto.Name;
        tenant.IsActive = dto.IsActive;
        tenant.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<TenantAgentKeyDto> GetAgentKeyAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FindAsync(new object?[] { id }, ct) ?? throw new NotFoundException(nameof(Tenant), id);
        return new TenantAgentKeyDto(tenant.AgentApiKey, _agentNotifier.EstaConectado(id));
    }

    public async Task<TenantAgentKeyDto> RegenerateAgentKeyAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FindAsync(new object?[] { id }, ct) ?? throw new NotFoundException(nameof(Tenant), id);
        tenant.AgentApiKey = Guid.NewGuid().ToString("N");
        await _db.SaveChangesAsync(ct);
        return new TenantAgentKeyDto(tenant.AgentApiKey, false);
    }
}
