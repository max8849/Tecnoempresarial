import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AccessPass, AccessPassCreated } from '../models/models';

// Sin fechas ni usos: esa política la fija el backend siempre (1 solo uso, hoy a +24h).
export interface AccessPassCreateDto {
  houseId: string;
  visitorName: string;
  vehiclePlate: string | null;
  notes: string | null;
}

@Injectable({ providedIn: 'root' })
export class AccessPassService {
  private base = `${environment.apiBaseUrl}/access-passes`;

  constructor(private http: HttpClient) {}

  listar(houseId?: string) {
    return this.http.get<AccessPass[]>(this.base, { params: houseId ? { houseId } : {} });
  }

  crear(dto: AccessPassCreateDto) { return this.http.post<AccessPassCreated>(this.base, dto); }

  revocar(id: string) { return this.http.post<void>(`${this.base}/${id}/revoke`, {}); }
}
