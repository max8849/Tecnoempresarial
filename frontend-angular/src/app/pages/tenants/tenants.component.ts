import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TenantAgentKey, TenantService } from '../../core/services/tenant.service';
import { Tenant } from '../../core/models/models';

declare const bootstrap: any;

@Component({
  selector: 'app-tenants',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './tenants.component.html'
})
export class TenantsComponent implements OnInit {
  tenants: Tenant[] = [];
  claveAgente: (TenantAgentKey & { tenantName: string }) | null = null;

  private fb = inject(FormBuilder);
  form = this.fb.group({ name: ['', Validators.required], slug: ['', Validators.required] });

  constructor(private service: TenantService) {}

  ngOnInit(): void { this.cargar(); }

  cargar(): void { this.service.listar().subscribe(t => this.tenants = t); }

  abrirModal(): void {
    this.form.reset();
    new bootstrap.Modal(document.getElementById('modalTenant')).show();
  }

  onGuardar(): void {
    if (this.form.invalid) return;
    const v = this.form.value;
    this.service.crear({ name: v.name!, slug: v.slug! }).subscribe(() => {
      bootstrap.Modal.getInstance(document.getElementById('modalTenant'))?.hide();
      this.cargar();
    });
  }

  verClaveAgente(tenant: Tenant): void {
    this.service.obtenerClaveAgente(tenant.id).subscribe(clave => {
      this.claveAgente = { ...clave, tenantName: tenant.name };
      new bootstrap.Modal(document.getElementById('modalClaveAgente')).show();
    });
  }

  regenerarClaveAgente(tenant: Tenant): void {
    if (!confirm(`¿Regenerar la clave del agente de "${tenant.name}"? El agente instalado dejará de poder conectarse hasta que actualices su configuración.`)) return;
    this.service.regenerarClaveAgente(tenant.id).subscribe(clave => {
      this.claveAgente = { ...clave, tenantName: tenant.name };
      new bootstrap.Modal(document.getElementById('modalClaveAgente')).show();
    });
  }
}
