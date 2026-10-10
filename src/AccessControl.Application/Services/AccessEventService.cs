using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Application.Services;

public interface IAccessEventService
{
    Task<List<AccessEventDto>> GetRecentAsync(int limit, CancellationToken ct = default);
}

public class AccessEventService : IAccessEventService
{
    private readonly IAppDbContext _db;

    public AccessEventService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<AccessEventDto>> GetRecentAsync(int limit, CancellationToken ct = default)
    {
        return await _db.AccessEvents
            .Include(e => e.AccessPoint)
            .Include(e => e.Device)
            .Include(e => e.AccessPass)
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .Select(e => new AccessEventDto(
                e.Id, e.AccessPoint!.Name, e.Device!.Name, e.AccessPass!.VisitorName,
                e.EventType, e.Result, e.Timestamp, e.Metadata))
            .ToListAsync(ct);
    }
}
