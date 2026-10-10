import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { InventoryService } from '../../core/services/inventory.service';
import { TenantService } from '../../core/services/tenant.service';
import { AuthService } from '../../core/services/auth.service';
import { InventoryItem, Tenant } from '../../core/models/models';
import { exportarInventarioExcel } from '../../core/utils/inventario-excel';

declare const bootstrap: any;

const ETIQUETAS_CATEGORIA: Record<string, string> = {
  GateMotor: 'Motor de portón', Barrier: 'Pluma / Barrera', HikvisionPanel: 'Panel de control',
  Camera: 'Cámara', Reader: 'Lector', Relay: 'Relay', NetworkEquipment: 'Equipo de red',
  Ups: 'UPS', Cabling: 'Cableado', Other: 'Otro'
};
const ETIQUETAS_ESTADO: Record<string, string> = {
  Active: 'Activo', InRepair: 'En reparación', InStorage: 'En almacén', Decommissioned: 'Dado de baja'
};

@Component({
  selector: 'app-inventario',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './inventario.component.html'
})
export class InventarioComponent implements OnInit {
  items: InventoryItem[] = [];
  tenants: Tenant[] = [];
  tenantFiltro = '';
  editandoId: string | null = null;

  etiquetaCategoria = ETIQUETAS_CATEGORIA;
  etiquetaEstado = ETIQUETAS_ESTADO;

  private fb = inject(FormBuilder);

  form = this.fb.group({
    tenantId: [''],
    category: ['Barrier', Validators.required],
    name: ['', Validators.required],
    brand: [''],
    model: [''],
    serialNumber: [''],
    status: ['Active'],
    location: [''],
    installationDate: [''],
    purchaseDate: [''],
    warrantyUntil: [''],
    provider: [''],
    cost: [null as number | null],
    notes: ['']
  });

  constructor(
    private inventory: InventoryService,
    private tenantService: TenantService, public auth: AuthService
  ) {}

  marcaModelo(item: InventoryItem): string {
    return [item.brand, item.model].filter(v => !!v).join(' / ') || '-';
  }

  get esSuperAdmin(): boolean { return this.auth.tieneRol('SUPER_ADMIN'); }

  ngOnInit(): void {
    if (this.esSuperAdmin) this.tenantService.listar().subscribe(t => this.tenants = t);
    this.cargar();
  }

  cargar(): void {
    this.inventory.listar(this.esSuperAdmin ? (this.tenantFiltro || null) : null).subscribe(items => this.items = items);
  }

  abrirModalNuevo(): void {
    this.editandoId = null;
    this.form.reset({ category: 'Barrier', status: 'Active' });
    new bootstrap.Modal(document.getElementById('modalInventario')).show();
  }

  abrirModalEditar(item: InventoryItem): void {
    this.editandoId = item.id;
    this.form.reset({
      tenantId: item.tenantId, category: item.category, name: item.name, brand: item.brand || '',
      model: item.model || '', serialNumber: item.serialNumber || '', status: item.status,
      location: item.location || '', installationDate: item.installationDate?.substring(0, 10) || '',
      purchaseDate: item.purchaseDate?.substring(0, 10) || '', warrantyUntil: item.warrantyUntil?.substring(0, 10) || '',
      provider: item.provider || '', cost: item.cost, notes: item.notes || ''
    });
    new bootstrap.Modal(document.getElementById('modalInventario')).show();
  }

  onGuardar(): void {
    if (this.form.invalid) return;
    const v = this.form.value;
    const base = {
      category: v.category!, name: v.name!, brand: v.brand || null, model: v.model || null,
      serialNumber: v.serialNumber || null, location: v.location || null,
      installationDate: v.installationDate || null, purchaseDate: v.purchaseDate || null,
      warrantyUntil: v.warrantyUntil || null, provider: v.provider || null,
      cost: v.cost != null && v.cost !== ('' as any) ? Number(v.cost) : null, notes: v.notes || null,
      linkedDeviceId: null
    };

    const cerrar = () => { bootstrap.Modal.getInstance(document.getElementById('modalInventario'))?.hide(); this.cargar(); };

    if (this.editandoId) {
      this.inventory.actualizar(this.editandoId, { ...base, status: v.status! }).subscribe(cerrar);
    } else {
      this.inventory.crear({ ...base, tenantId: v.tenantId || null }).subscribe(cerrar);
    }
  }

  exportarExcel(): void {
    exportarInventarioExcel(this.items, !this.tenantFiltro);
  }

  eliminar(id: string): void {
    if (!confirm('¿Eliminar este ítem del inventario?')) return;
    this.inventory.eliminar(id).subscribe(() => this.cargar());
  }
}
