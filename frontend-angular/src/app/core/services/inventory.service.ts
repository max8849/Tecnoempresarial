import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { InventoryItem } from '../models/models';

export interface InventoryCreateDto {
  tenantId: string | null;
  category: string; name: string; brand: string | null; model: string | null; serialNumber: string | null;
  location: string | null; installationDate: string | null; purchaseDate: string | null; warrantyUntil: string | null;
  provider: string | null; cost: number | null; notes: string | null; linkedDeviceId: string | null;
}

export interface InventoryUpdateDto extends Omit<InventoryCreateDto, 'tenantId'> {
  status: string;
}

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private base = `${environment.apiBaseUrl}/inventory`;

  constructor(private http: HttpClient) {}

  listar(tenantId?: string | null) {
    return this.http.get<InventoryItem[]>(this.base, { params: tenantId ? { tenantId } : {} });
  }

  crear(dto: InventoryCreateDto) { return this.http.post<InventoryItem>(this.base, dto); }

  actualizar(id: string, dto: InventoryUpdateDto) { return this.http.put<void>(`${this.base}/${id}`, dto); }

  eliminar(id: string) { return this.http.delete<void>(`${this.base}/${id}`); }
}
