import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ResidenteSinCuenta, UsuarioCuenta } from '../models/models';

export interface CrearUsuarioDto {
  email: string;
  fullName: string;
  password: string;
  role: string;
  tenantId: string | null;
  residentId: string | null;
}

@Injectable({ providedIn: 'root' })
export class UsuarioService {
  private base = `${environment.apiBaseUrl}/usuarios`;

  constructor(private http: HttpClient) {}

  listar() { return this.http.get<UsuarioCuenta[]>(this.base); }

  residentesDisponibles() { return this.http.get<ResidenteSinCuenta[]>(`${this.base}/residentes-disponibles`); }

  crear(dto: CrearUsuarioDto) { return this.http.post(this.base, dto); }

  desactivar(id: string) { return this.http.post<void>(`${this.base}/${id}/desactivar`, {}); }

  activar(id: string) { return this.http.post<void>(`${this.base}/${id}/activar`, {}); }

  restablecerContrasena(id: string, nuevaContrasena: string) {
    return this.http.post<void>(`${this.base}/${id}/restablecer-contrasena`, { nuevaContrasena });
  }
}
