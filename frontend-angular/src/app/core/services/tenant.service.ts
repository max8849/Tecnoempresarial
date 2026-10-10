import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Tenant } from '../models/models';

export interface TenantAgentKey { agentApiKey: string; agentConectado: boolean; }

@Injectable({ providedIn: 'root' })
export class TenantService {
  private base = `${environment.apiBaseUrl}/tenants`;

  constructor(private http: HttpClient) {}

  listar() { return this.http.get<Tenant[]>(this.base); }

  crear(dto: { name: string; slug: string }) { return this.http.post<Tenant>(this.base, dto); }

  obtenerClaveAgente(tenantId: string) { return this.http.get<TenantAgentKey>(`${this.base}/${tenantId}/agent-key`); }

  regenerarClaveAgente(tenantId: string) { return this.http.post<TenantAgentKey>(`${this.base}/${tenantId}/agent-key/regenerate`, {}); }
}
