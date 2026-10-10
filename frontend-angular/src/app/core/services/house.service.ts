import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { House } from '../models/models';

@Injectable({ providedIn: 'root' })
export class HouseService {
  private base = `${environment.apiBaseUrl}/houses`;

  constructor(private http: HttpClient) {}

  listar() { return this.http.get<House[]>(this.base); }

  crear(dto: { number: string; notes: string | null; tenantId?: string | null }) { return this.http.post<House>(this.base, dto); }

  actualizar(id: string, dto: { number: string; notes: string | null; status: string }) {
    return this.http.put<void>(`${this.base}/${id}`, dto);
  }
}
