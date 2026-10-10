namespace AccessControl.Application.DTOs;

public record VehicleDto(Guid Id, Guid HouseId, string Plate, string? Brand, string? Model, string? Color);

public record VehicleCreateDto(Guid HouseId, string Plate, string? Brand, string? Model, string? Color);
