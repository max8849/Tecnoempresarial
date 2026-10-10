import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { AccessPointService } from '../../core/services/access-point.service';
import { DeviceService } from '../../core/services/device.service';
import { TenantService } from '../../core/services/tenant.service';
import { AuthService } from '../../core/services/auth.service';
import { AccessPoint, Device, Tenant } from '../../core/models/models';

declare const bootstrap: any;

@Component({
  selector: 'app-dispositivos',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './dispositivos.component.html'
})
export class DispositivosComponent implements OnInit {
  puntos: AccessPoint[] = [];
  dispositivos: Device[] = [];
  tenants: Tenant[] = [];
  tenantFiltro = '';
  probando: Record<string, boolean> = {};
  resultadoPrueba: Record<string, string> = {};
  errorPunto: string | null = null;
  errorDevice: string | null = null;

  private fb = inject(FormBuilder);

  formPunto = this.fb.group({
    tenantId: [''],
    name: ['', Validators.required],
    type: ['VehicularGate', Validators.required],
    direction: ['Both', Validators.required]
  });

  formDevice = this.fb.group({
    tenantId: [''],
    name: ['', Validators.required],
    manufacturer: [''],
    driverType: ['Mock', Validators.required],
    ipAddress: [''],
    username: [''],
    password: [''],
    port: [80],
    useHttps: [false],
    accessPointId: [''],
    channelNumber: [1, Validators.required]
  });

  constructor(
    private accessPoints: AccessPointService,
    private devices: DeviceService,
    private tenantService: TenantService,
    public auth: AuthService
  ) {}

  get esSuperAdmin(): boolean { return this.auth.tieneRol('SUPER_ADMIN'); }

  get esDriverConCredenciales(): boolean {
    return this.formDevice.value.driverType === 'Hikvision' || this.formDevice.value.driverType === 'HikvisionViaAgente';
  }

  get puntosVisibles(): AccessPoint[] {
    return this.tenantFiltro ? this.puntos.filter(p => p.tenantId === this.tenantFiltro) : this.puntos;
  }

  get dispositivosVisibles(): Device[] {
    return this.tenantFiltro ? this.dispositivos.filter(d => d.tenantId === this.tenantFiltro) : this.dispositivos;
  }

  get puntosDelFormulario(): AccessPoint[] {
    if (!this.esSuperAdmin) return this.puntos;
    const privada = this.formDevice.value.tenantId;
    return privada ? this.puntos.filter(p => p.tenantId === privada) : [];
  }

  ngOnInit(): void {
    if (this.esSuperAdmin) {
      this.formPunto.controls.tenantId.addValidators(Validators.required);
      this.formDevice.controls.tenantId.addValidators(Validators.required);
      this.tenantService.listar().subscribe(t => this.tenants = t);
    }
    this.cargar();
  }

  cargar(): void {
    forkJoin({ puntos: this.accessPoints.listar(), dispositivos: this.devices.listar() })
      .subscribe(({ puntos, dispositivos }) => { this.puntos = puntos; this.dispositivos = dispositivos; });
  }

  onCambioPrivadaDevice(): void {
    this.formDevice.patchValue({ accessPointId: '' });
  }

  abrirModalPunto(): void {
    this.errorPunto = null;
    this.formPunto.reset({ tenantId: this.tenantFiltro || '', type: 'VehicularGate', direction: 'Both' });
    new bootstrap.Modal(document.getElementById('modalAccessPoint')).show();
  }

  onGuardarPunto(): void {
    if (this.formPunto.invalid) return;
    const v = this.formPunto.value;
    this.errorPunto = null;
    this.accessPoints.crear({ name: v.name!, type: v.type!, direction: v.direction!, tenantId: v.tenantId || null }).subscribe({
      next: () => {
        bootstrap.Modal.getInstance(document.getElementById('modalAccessPoint'))?.hide();
        this.cargar();
      },
      error: err => this.errorPunto = err.error?.mensaje ?? 'No se pudo guardar el punto de acceso.'
    });
  }

  abrirModalDevice(): void {
    this.errorDevice = null;
    this.formDevice.reset({ tenantId: this.tenantFiltro || '', driverType: 'Mock', channelNumber: 1, port: 80, useHttps: false });
    new bootstrap.Modal(document.getElementById('modalDevice')).show();
  }

  onGuardarDevice(): void {
    if (this.formDevice.invalid) return;
    const v = this.formDevice.value;
    this.errorDevice = null;
    this.devices.crear({
      name: v.name!, manufacturer: v.manufacturer || null, model: null,
      ipAddress: v.ipAddress || null, driverType: v.driverType!,
      username: v.username || null, password: v.password || null,
      port: v.port ? Number(v.port) : null, useHttps: !!v.useHttps,
      tenantId: v.tenantId || null
    }).subscribe({
      next: device => {
        const vincular = v.accessPointId
          ? this.devices.vincular(device.id, { accessPointId: v.accessPointId!, channelNumber: Number(v.channelNumber) })
          : null;

        const finalizar = () => {
          bootstrap.Modal.getInstance(document.getElementById('modalDevice'))?.hide();
          this.cargar();
        };
        if (vincular) {
          vincular.subscribe({
            next: finalizar,
            error: err => { this.errorDevice = err.error?.mensaje ?? 'El dispositivo se creó, pero no se pudo vincular al punto.'; this.cargar(); }
          });
        } else {
          finalizar();
        }
      },
      error: err => this.errorDevice = err.error?.mensaje ?? 'No se pudo guardar el dispositivo.'
    });
  }

  nombresPuntos(d: Device): string {
    return d.accessPoints.length ? d.accessPoints.map(p => `${p.accessPointName} (canal ${p.channelNumber})`).join(', ') : '-';
  }

  probarConexion(device: Device): void {
    this.probando[device.id] = true;
    delete this.resultadoPrueba[device.id];
    this.devices.probarConexion(device.id).subscribe({
      next: r => {
        this.probando[device.id] = false;
        this.resultadoPrueba[device.id] = r.reachable ? '✅ Conectó correctamente' : `❌ No se pudo conectar: ${r.details || 'sin detalle'}`;
        this.cargar();
      },
      error: err => {
        this.probando[device.id] = false;
        this.resultadoPrueba[device.id] = '❌ ' + (err.error?.mensaje || 'Error al probar la conexión.');
      }
    });
  }
}
