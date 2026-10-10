namespace AccessControl.Application.DTOs;

public record AgentTokenRequestDto(string AgentApiKey);

public record AgentTokenResponseDto(string AccessToken, DateTime ExpiresAt, string TenantName);

public record TenantAgentKeyDto(string AgentApiKey, bool AgentConectado);
