import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Resident } from '../models/models';

@Injectable({ providedIn: 'root' })
export class ResidentService {
  private base = `${environment.apiBaseUrl}/residents`;

  constructor(private http: HttpClient) {}

  listar(houseId?: string) {
    return this.http.get<Resident[]>(this.base, { params: houseId ? { houseId } : {} });
  }

  crear(dto: { houseId: string; firstName: string; lastName: string; phone: string | null; email: string | null }) {
    return this.http.post<Resident>(this.base, dto);
  }
}
