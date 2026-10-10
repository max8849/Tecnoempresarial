import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { Rol } from '../../core/models/models';

declare const bootstrap: any;

const PATRON_CONTRASENA = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{10,}$/;

interface ItemMenu {
  seccionLabel?: string;
  ruta: string;
  icono: string;
  texto: string;
  roles: Rol[];
}

const MENU: ItemMenu[] = [
  { seccionLabel: 'Panel', ruta: '/dashboard', icono: 'bi-speedometer2', texto: 'Dashboard', roles: ['ADMIN_PRIVADA', 'SUPER_ADMIN'] },
  { seccionLabel: 'Mi cuenta', ruta: '/mis-qrs', icono: 'bi-qr-code', texto: 'Mis Visitantes / QRs', roles: ['RESIDENTE'] },
  { seccionLabel: 'Caseta', ruta: '/escanear', icono: 'bi-camera', texto: 'Escanear QR', roles: ['GUARDIA', 'ADMIN_PRIVADA', 'SUPER_ADMIN'] },
  { seccionLabel: 'Administración de la privada', ruta: '/casas', icono: 'bi-house-door', texto: 'Casas', roles: ['ADMIN_PRIVADA', 'SUPER_ADMIN'] },
  { ruta: '/residentes', icono: 'bi-people', texto: 'Residentes', roles: ['ADMIN_PRIVADA', 'SUPER_ADMIN'] },
  { ruta: '/usuarios', icono: 'bi-person-badge', texto: 'Cuentas de acceso', roles: ['ADMIN_PRIVADA', 'SUPER_ADMIN'] },
  { ruta: '/dispositivos', icono: 'bi-hdd-network', texto: 'Puntos de Acceso / Dispositivos', roles: ['ADMIN_PRIVADA', 'SUPER_ADMIN'] },
  { seccionLabel: 'Inventario', ruta: '/inventario', icono: 'bi-box-seam', texto: 'Equipo e Inventario', roles: ['SUPER_ADMIN'] },
  { seccionLabel: 'Auditoría', ruta: '/eventos', icono: 'bi-clock-history', texto: 'Historial de Accesos', roles: ['ADMIN_PRIVADA', 'GUARDIA', 'SUPER_ADMIN'] },
  { seccionLabel: 'Sistema', ruta: '/tenants', icono: 'bi-buildings', texto: 'Privadas', roles: ['SUPER_ADMIN'] }
];

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent {
  sidebarAbierta = false;
  errorContrasena: string | null = null;
  exitoContrasena = false;

  private fb = inject(FormBuilder);
  formContrasena = this.fb.group({
    contrasenaActual: ['', Validators.required],
    contrasenaNueva: ['', [Validators.required, Validators.pattern(PATRON_CONTRASENA)]]
  });

  // computed() memoiza según el signal `sesion`: solo se recalcula cuando la sesión
  // realmente cambia, no en cada ciclo de detección de cambios. Un getter plano aquí
  // devolvía un array nuevo en cada evaluación, haciendo que *ngFor destruyera y
  // recreara el menú constantemente — con RouterLinkActive de por medio, eso podía
  // encadenar más detecciones de cambio y colgar la pestaña.
  menuVisible = computed<ItemMenu[]>(() => {
    const rol = this.auth.sesion()?.role;
    return MENU.filter(m => rol && m.roles.includes(rol));
  });

  constructor(public auth: AuthService, private router: Router) {}

  etiquetaRol(rol: Rol | undefined): string {
    if (!rol) return '';
    const etiquetas: Record<Rol, string> = {
      SUPER_ADMIN: 'Super Administrador', ADMIN_PRIVADA: 'Administrador de Privada',
      RESIDENTE: 'Residente', GUARDIA: 'Guardia', OPERADOR: 'Operador'
    };
    return etiquetas[rol];
  }

  abrirCambioContrasena(): void {
    this.errorContrasena = null;
    this.exitoContrasena = false;
    this.formContrasena.reset({ contrasenaActual: '', contrasenaNueva: '' });
    new bootstrap.Modal(document.getElementById('modalMiContrasena')).show();
  }

  guardarCambioContrasena(): void {
    if (this.formContrasena.invalid) return;
    const v = this.formContrasena.value;
    this.errorContrasena = null;
    this.auth.cambiarContrasena(v.contrasenaActual!, v.contrasenaNueva!).subscribe({
      next: () => {
        this.exitoContrasena = true;
        setTimeout(() => bootstrap.Modal.getInstance(document.getElementById('modalMiContrasena'))?.hide(), 1200);
      },
      error: err => this.errorContrasena = err.error?.mensaje ?? 'No se pudo cambiar la contraseña.'
    });
  }

  toggleSidebar(): void { this.sidebarAbierta = !this.sidebarAbierta; }
  cerrarSidebar(): void { this.sidebarAbierta = false; }

  salir(): void {
    this.auth.logout();
    this.router.navigateByUrl('/login');
  }
}
