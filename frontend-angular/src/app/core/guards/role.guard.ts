import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { Rol } from '../models/models';

// Uso: { path: 'tenants', canActivate: [roleGuard(['SUPER_ADMIN'])], component: ... }
export function roleGuard(rolesPermitidos: Rol[]): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    if (auth.tieneRol(...rolesPermitidos)) return true;
    // Redirige a LA RUTA DE ESE ROL, nunca a una fija: mandar siempre a '/dashboard'
    // provocaba un bucle infinito para roles (RESIDENTE, GUARDIA) que no tienen acceso
    // a esa ruta, ya que su propio guard los rebotaba de vuelta ahí mismo sin parar.
    router.navigateByUrl(auth.rutaInicio());
    return false;
  };
}
