import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginResponse, Rol, Sesion } from '../models/models';

const CLAVE_STORAGE = 'access_sesion';

@Injectable({ providedIn: 'root' })
export class AuthService {
  // Signal reactivo: los componentes (ej. el shell/sidebar) pueden reaccionar a cambios de sesión sin suscribirse manualmente.
  sesion = signal<Sesion | null>(this.leerStorage());

  constructor(private http: HttpClient) {}

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiBaseUrl}/auth/login`, { email, password }).pipe(
      tap(resp => this.guardarSesion(resp))
    );
  }

  refresh(): Observable<LoginResponse> {
    const actual = this.sesion();
    return this.http.post<LoginResponse>(`${environment.apiBaseUrl}/auth/refresh`, { refreshToken: actual?.refreshToken }).pipe(
      tap(resp => this.guardarSesion(resp))
    );
  }

  cambiarContrasena(contrasenaActual: string, contrasenaNueva: string): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/auth/cambiar-contrasena`, { contrasenaActual, contrasenaNueva });
  }

  logout(): void {
    localStorage.removeItem(CLAVE_STORAGE);
    this.sesion.set(null);
  }

  estaAutenticado(): boolean {
    return !!this.sesion();
  }

  tieneRol(...roles: Rol[]): boolean {
    const rol = this.sesion()?.role;
    return !!rol && roles.includes(rol);
  }

  // Ruta "de inicio" correcta según el rol de la sesión actual. Se usa tanto al terminar
  // el login como en el fallback de roleGuard, para nunca redirigir a una ruta que el
  // propio rol no tiene permiso de ver (eso fue justo lo que causaba el bucle infinito
  // cuando un RESIDENTE terminaba mandado a /dashboard, que le está vedado).
  rutaInicio(): string {
    const rol = this.sesion()?.role;
    switch (rol) {
      case 'SUPER_ADMIN': return '/tenants';
      case 'ADMIN_PRIVADA': return '/dashboard';
      case 'RESIDENTE': return '/mis-qrs';
      case 'GUARDIA': return '/escanear';
      default: return '/login';
    }
  }

  private guardarSesion(resp: LoginResponse): void {
    localStorage.setItem(CLAVE_STORAGE, JSON.stringify(resp));
    this.sesion.set(resp);
  }

  private leerStorage(): Sesion | null {
    const raw = localStorage.getItem(CLAVE_STORAGE);
    return raw ? JSON.parse(raw) : null;
  }
}
