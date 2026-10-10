import { inject } from '@angular/core';
import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { AuthService } from './core/services/auth.service';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent) },
  {
    path: '',
    loadComponent: () => import('./layout/shell/shell.component').then(m => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent), canActivate: [roleGuard(['ADMIN_PRIVADA', 'SUPER_ADMIN'])] },
      { path: 'mis-qrs', loadComponent: () => import('./pages/mis-qrs/mis-qrs.component').then(m => m.MisQrsComponent), canActivate: [roleGuard(['RESIDENTE'])] },
      { path: 'escanear', loadComponent: () => import('./pages/escanear/escanear.component').then(m => m.EscanearComponent), canActivate: [roleGuard(['GUARDIA', 'ADMIN_PRIVADA', 'SUPER_ADMIN'])] },
      { path: 'casas', loadComponent: () => import('./pages/casas/casas.component').then(m => m.CasasComponent), canActivate: [roleGuard(['ADMIN_PRIVADA', 'SUPER_ADMIN'])] },
      { path: 'residentes', loadComponent: () => import('./pages/residentes/residentes.component').then(m => m.ResidentesComponent), canActivate: [roleGuard(['ADMIN_PRIVADA', 'SUPER_ADMIN'])] },
      { path: 'dispositivos', loadComponent: () => import('./pages/dispositivos/dispositivos.component').then(m => m.DispositivosComponent), canActivate: [roleGuard(['ADMIN_PRIVADA', 'SUPER_ADMIN'])] },
      { path: 'inventario', loadComponent: () => import('./pages/inventario/inventario.component').then(m => m.InventarioComponent), canActivate: [roleGuard(['SUPER_ADMIN'])] },
      { path: 'eventos', loadComponent: () => import('./pages/eventos/eventos.component').then(m => m.EventosComponent), canActivate: [roleGuard(['ADMIN_PRIVADA', 'GUARDIA', 'SUPER_ADMIN'])] },
      { path: 'usuarios', loadComponent: () => import('./pages/usuarios/usuarios.component').then(m => m.UsuariosComponent), canActivate: [roleGuard(['ADMIN_PRIVADA', 'SUPER_ADMIN'])] },
      { path: 'tenants', loadComponent: () => import('./pages/tenants/tenants.component').then(m => m.TenantsComponent), canActivate: [roleGuard(['SUPER_ADMIN'])] },
      // Redirige a la ruta correcta según el rol de la sesión, nunca a una fija
      // (una ruta fija aquí reproduce el mismo bucle infinito que tenían roleGuard/login).
      { path: '', pathMatch: 'full', redirectTo: () => inject(AuthService).rutaInicio() }
    ]
  },
  { path: '**', redirectTo: 'login' }
];
