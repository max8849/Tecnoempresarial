import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { HouseService } from '../../core/services/house.service';
import { TenantService } from '../../core/services/tenant.service';
import { AuthService } from '../../core/services/auth.service';
import { House, Tenant } from '../../core/models/models';

declare const bootstrap: any;

@Component({
  selector: 'app-casas',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './casas.component.html'
})
export class CasasComponent implements OnInit {
  casas: House[] = [];
  tenants: Tenant[] = [];
  tenantFiltro = '';
  error: string | null = null;

  private fb = inject(FormBuilder);
  form = this.fb.group({ tenantId: [''], number: ['', Validators.required], notes: [''] });

  constructor(private houses: HouseService, private tenantService: TenantService, public auth: AuthService) {}

  get esSuperAdmin(): boolean { return this.auth.tieneRol('SUPER_ADMIN'); }

  get casasVisibles(): House[] {
    return this.tenantFiltro ? this.casas.filter(c => c.tenantId === this.tenantFiltro) : this.casas;
  }

  ngOnInit(): void {
    if (this.esSuperAdmin) {
      this.form.controls.tenantId.addValidators(Validators.required);
      this.tenantService.listar().subscribe(t => this.tenants = t);
    }
    this.cargar();
  }

  cargar(): void { this.houses.listar().subscribe(c => this.casas = c); }

  abrirModal(): void {
    this.error = null;
    this.form.reset({ tenantId: this.tenantFiltro || '' });
    new bootstrap.Modal(document.getElementById('modalCasa')).show();
  }

  onGuardar(): void {
    if (this.form.invalid) return;
    const v = this.form.value;
    this.error = null;
    this.houses.crear({ number: v.number!, notes: v.notes || null, tenantId: v.tenantId || null }).subscribe({
      next: () => {
        bootstrap.Modal.getInstance(document.getElementById('modalCasa'))?.hide();
        this.cargar();
      },
      error: err => this.error = err.error?.mensaje ?? 'No se pudo guardar la casa.'
    });
  }
}
