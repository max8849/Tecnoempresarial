import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { UsuarioService } from '../../core/services/usuario.service';
import { TenantService } from '../../core/services/tenant.service';
import { AuthService } from '../../core/services/auth.service';
import { ResidenteSinCuenta, Tenant, UsuarioCuenta } from '../../core/models/models';

declare const bootstrap: any;

const PATRON_CONTRASENA = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{10,}$/;

const ETIQUETAS_ROL: Record<string, string> = {
  SUPER_ADMIN: 'Superadmin',
  ADMIN_PRIVADA: 'Administrador de privada',
  RESIDENTE: 'Residente',
  GUARDIA: 'Guardia'
};

@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './usuarios.component.html'
})
export class UsuariosComponent implements OnInit {
  usuarios: UsuarioCuenta[] = [];
  residentes: ResidenteSinCuenta[] = [];
  tenants: Tenant[] = [];
  tenantFiltro = '';
  error: string | null = null;
  exito: string | null = null;
  errorModal: string | null = null;
  usuarioRestablecer: UsuarioCuenta | null = null;

  private fb = inject(FormBuilder);

  form = this.fb.group({
    role: ['RESIDENTE', Validators.required],
    tenantId: [''],
    residentId: [''],
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.pattern(PATRON_CONTRASENA)]]
  });

  formRestablecer = this.fb.group({
    password: ['', [Validators.required, Validators.pattern(PATRON_CONTRASENA)]]
  });

  constructor(private usuariosSvc: UsuarioService, private tenantService: TenantService, public auth: AuthService) {}

  get esSuperAdmin(): boolean { return this.auth.tieneRol('SUPER_ADMIN'); }

  get rolesPermitidos(): { valor: string; etiqueta: string }[] {
    const todos = Object.entries(ETIQUETAS_ROL).map(([valor, etiqueta]) => ({ valor, etiqueta }));
    return this.esSuperAdmin ? todos : todos.filter(r => r.valor === 'RESIDENTE' || r.valor === 'GUARDIA');
  }

  etiquetaRol(rol: string): string { return ETIQUETAS_ROL[rol] ?? rol; }

  etiquetaResidente(r: ResidenteSinCuenta): string {
    const base = `${r.nombreCompleto} · Casa ${r.casa}`;
    return this.esSuperAdmin ? `${base} (${r.tenantName})` : base;
  }

  get usuariosVisibles(): UsuarioCuenta[] {
    return this.tenantFiltro ? this.usuarios.filter(u => u.tenantId === this.tenantFiltro) : this.usuarios;
  }

  get residentesDelFormulario(): ResidenteSinCuenta[] {
    const privada = this.form.value.tenantId;
    return this.esSuperAdmin && privada ? this.residentes.filter(r => r.tenantId === privada) : this.residentes;
  }

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.usuariosSvc.listar().subscribe(u => this.usuarios = u);
    this.usuariosSvc.residentesDisponibles().subscribe(r => this.residentes = r);
    if (this.esSuperAdmin) this.tenantService.listar().subscribe(t => this.tenants = t);
  }

  abrirModalNuevo(): void {
    this.errorModal = null;
    this.form.reset({ role: 'RESIDENTE', tenantId: this.tenantFiltro || '', residentId: '', fullName: '', email: '', password: '' });
    this.aplicarReglasDeRol();
    new bootstrap.Modal(document.getElementById('modalCuenta')).show();
  }

  onRolCambio(): void {
    this.form.patchValue({ residentId: '' });
    this.aplicarReglasDeRol();
  }

  onPrivadaCambio(): void {
    this.form.patchValue({ residentId: '' });
  }

  onResidenteCambio(): void {
    const id = this.form.value.residentId;
    const residente = this.residentes.find(r => r.id === id);
    if (residente && !this.form.value.fullName) this.form.patchValue({ fullName: residente.nombreCompleto });
  }

  onGuardar(): void {
    if (this.form.invalid) return;
    const v = this.form.value;
    this.errorModal = null;
    this.usuariosSvc.crear({
      email: v.email!.trim(),
      fullName: v.fullName!.trim(),
      password: v.password!,
      role: v.role!,
      tenantId: this.esSuperAdmin && v.role !== 'SUPER_ADMIN' ? v.tenantId || null : null,
      residentId: v.role === 'RESIDENTE' ? v.residentId || null : null
    }).subscribe({
      next: () => {
        bootstrap.Modal.getInstance(document.getElementById('modalCuenta'))?.hide();
        this.mostrarExito('Cuenta creada. Entrega el correo y la contraseña inicial a la persona.');
        this.cargar();
      },
      error: err => this.errorModal = err.error?.mensaje ?? 'No se pudo crear la cuenta.'
    });
  }

  alternarActivo(u: UsuarioCuenta): void {
    const accion = u.activo ? 'desactivar' : 'activar';
    if (u.activo && !confirm(`¿Desactivar la cuenta de ${u.fullName}? Perderá el acceso de inmediato.`)) return;
    const peticion = u.activo ? this.usuariosSvc.desactivar(u.id) : this.usuariosSvc.activar(u.id);
    peticion.subscribe({
      next: () => { this.mostrarExito(`Cuenta ${u.activo ? 'desactivada' : 'activada'}.`); this.cargar(); },
      error: err => this.error = err.error?.mensaje ?? `No se pudo ${accion} la cuenta.`
    });
  }

  abrirRestablecer(u: UsuarioCuenta): void {
    this.usuarioRestablecer = u;
    this.errorModal = null;
    this.formRestablecer.reset({ password: '' });
    new bootstrap.Modal(document.getElementById('modalRestablecer')).show();
  }

  onRestablecer(): void {
    if (this.formRestablecer.invalid || !this.usuarioRestablecer) return;
    this.errorModal = null;
    this.usuariosSvc.restablecerContrasena(this.usuarioRestablecer.id, this.formRestablecer.value.password!).subscribe({
      next: () => {
        bootstrap.Modal.getInstance(document.getElementById('modalRestablecer'))?.hide();
        this.mostrarExito(`Contraseña de ${this.usuarioRestablecer?.fullName} restablecida. Sus sesiones abiertas se cerraron.`);
      },
      error: err => this.errorModal = err.error?.mensaje ?? 'No se pudo restablecer la contraseña.'
    });
  }

  private aplicarReglasDeRol(): void {
    const rol = this.form.value.role;
    const privada = this.form.controls.tenantId;
    privada.clearValidators();
    if (this.esSuperAdmin && rol !== 'SUPER_ADMIN') privada.addValidators(Validators.required);
    privada.updateValueAndValidity();

    const residente = this.form.controls.residentId;
    residente.clearValidators();
    if (rol === 'RESIDENTE') residente.addValidators(Validators.required);
    residente.updateValueAndValidity();
  }

  private mostrarExito(texto: string): void {
    this.exito = texto;
    this.error = null;
  }
}
