import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { ResidentService } from '../../core/services/resident.service';
import { HouseService } from '../../core/services/house.service';
import { TenantService } from '../../core/services/tenant.service';
import { AuthService } from '../../core/services/auth.service';
import { House, Resident, Tenant } from '../../core/models/models';

declare const bootstrap: any;

@Component({
  selector: 'app-residentes',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './residentes.component.html'
})
export class ResidentesComponent implements OnInit {
  residentes: Resident[] = [];
  casas: House[] = [];
  tenants: Tenant[] = [];
  tenantFiltro = '';
  error: string | null = null;

  private fb = inject(FormBuilder);

  form = this.fb.group({
    houseId: ['', Validators.required],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    phone: [''],
    email: ['']
  });

  constructor(
    private residentService: ResidentService,
    private houseService: HouseService,
    private tenantService: TenantService,
    public auth: AuthService
  ) {}

  get esSuperAdmin(): boolean { return this.auth.tieneRol('SUPER_ADMIN'); }

  get residentesVisibles(): Resident[] {
    return this.tenantFiltro ? this.residentes.filter(r => r.tenantId === this.tenantFiltro) : this.residentes;
  }

  get casasVisibles(): House[] {
    return this.tenantFiltro ? this.casas.filter(c => c.tenantId === this.tenantFiltro) : this.casas;
  }

  ngOnInit(): void {
    if (this.esSuperAdmin) this.tenantService.listar().subscribe(t => this.tenants = t);
    this.cargar();
  }

  cargar(): void {
    forkJoin({ residentes: this.residentService.listar(), casas: this.houseService.listar() })
      .subscribe(({ residentes, casas }) => { this.residentes = residentes; this.casas = casas; });
  }

  etiquetaCasa(c: House): string {
    return this.esSuperAdmin ? `${c.number} · ${c.tenantName}` : c.number;
  }

  abrirModal(): void {
    this.error = null;
    this.form.reset();
    new bootstrap.Modal(document.getElementById('modalResidente')).show();
  }

  onGuardar(): void {
    if (this.form.invalid) return;
    const v = this.form.value;
    this.error = null;
    this.residentService.crear({
      houseId: v.houseId!, firstName: v.firstName!, lastName: v.lastName!,
      phone: v.phone || null, email: v.email || null
    }).subscribe({
      next: () => {
        bootstrap.Modal.getInstance(document.getElementById('modalResidente'))?.hide();
        this.cargar();
      },
      error: err => this.error = err.error?.mensaje ?? 'No se pudo guardar el residente.'
    });
  }
}
