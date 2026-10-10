import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

// Agrega el JWT a cada request y, ante un 401, intenta refrescar el token una vez
// antes de cerrar sesión. Si dos requests fallan casi al mismo tiempo pueden disparar
// dos refresh en paralelo; para el tamaño de esta app no vale la pena la complejidad
// de encolar requests mientras se refresca (sí valdría en una app de mucho tráfico).
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  // Las peticiones al propio endpoint de login/refresh nunca deben pasar por la lógica
  // de reintento: si no fuera por esto, un refresh que a su vez falla con 401 dispararía
  // otro refresh, y otro, en un bucle infinito que congela la pestaña sin ningún error visible.
  const esRutaDeAuth = req.url.includes('/auth/login') || req.url.includes('/auth/refresh');

  const sesion = auth.sesion();
  const reqConToken = sesion
    ? req.clone({ setHeaders: { Authorization: `Bearer ${sesion.accessToken}` } })
    : req;

  return next(reqConToken).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && sesion && !esRutaDeAuth) {
        return auth.refresh().pipe(
          switchMap(nuevaSesion => {
            const reintento = req.clone({ setHeaders: { Authorization: `Bearer ${nuevaSesion.accessToken}` } });
            return next(reintento);
          }),
          catchError(errRefresh => {
            auth.logout();
            router.navigateByUrl('/login');
            return throwError(() => errRefresh);
          })
        );
      }
      return throwError(() => error);
    })
  );
};
