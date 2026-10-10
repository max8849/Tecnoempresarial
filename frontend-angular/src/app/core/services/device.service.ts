import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Device } from '../models/models';

export interface DeviceCreateDto {
  name: string; manufacturer: string | null; model: string | null; ipAddress: string | null; driverType: string;
  username: string | null; password: string | null; port: number | null; useHttps: boolean | null;
  tenantId?: string | null;
}

export interface DeviceTestConnectionResult { reachable: boolean; details: string | null; }

@Injectable({ providedIn: 'root' })
export class DeviceService {
  private base = `${environment.apiBaseUrl}/devices`;

  constructor(private http: HttpClient) {}

  listar() { return this.http.get<Device[]>(this.base); }

  crear(dto: DeviceCreateDto) { return this.http.post<Device>(this.base, dto); }

  vincular(deviceId: string, dto: { accessPointId: string; channelNumber: number }) {
    return this.http.post<void>(`${this.base}/${deviceId}/access-points`, dto);
  }

  probarConexion(deviceId: string) {
    return this.http.post<DeviceTestConnectionResult>(`${this.base}/${deviceId}/test-connection`, {});
  }
}
