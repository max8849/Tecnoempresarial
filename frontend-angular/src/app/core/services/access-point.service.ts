import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AccessPoint } from '../models/models';

@Injectable({ providedIn: 'root' })
export class AccessPointService {
  private base = `${environment.apiBaseUrl}/access-points`;

  constructor(private http: HttpClient) {}

  listar() { return this.http.get<AccessPoint[]>(this.base); }

  crear(dto: { name: string; type: string; direction: string; tenantId?: string | null }) { return this.http.post<AccessPoint>(this.base, dto); }
}
