import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    // withFetch: usa la Fetch API en vez de XMLHttpRequest. En este entorno se detectó
    // que algo (antivirus/extensión) intercepta o cuelga las peticiones hechas por XHR,
    // mientras que fetch() funciona sin problema.
    provideHttpClient(withInterceptors([authInterceptor]), withFetch())
  ]
};
