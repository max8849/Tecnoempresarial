import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AccessPassService } from '../../core/services/access-pass.service';
import { HouseService } from '../../core/services/house.service';
import { AccessPass } from '../../core/models/models';

declare const bootstrap: any;

@Component({
  selector: 'app-mis-qrs',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './mis-qrs.component.html'
})
export class MisQrsComponent implements OnInit {
  qrs: AccessPass[] = [];
  etiquetas: Record<string, string> = { Active: 'Activo', Used: 'Usado', Expired: 'Expirado', Revoked: 'Revocado', Disabled: 'Deshabilitado' };

  // Se reutiliza el mismo modal tanto para el QR recién creado como para "ver de nuevo"
  // uno ya existente: ambos casos solo necesitan la imagen y el nombre del visitante.
  qrParaMostrar: { visitorName: string; qrImageBase64: string } | null = null;

  private fb = inject(FormBuilder);

  form = this.fb.group({
    visitorName: ['', Validators.required],
    vehiclePlate: [''],
    notes: ['']
  });

  constructor(private passes: AccessPassService, private houses: HouseService) {}

  ngOnInit(): void { this.cargar(); }

  cargar(): void { this.passes.listar().subscribe(qrs => this.qrs = qrs); }

  abrirModalNuevo(): void {
    this.form.reset();
    new bootstrap.Modal(document.getElementById('modalNuevoQr')).show();
  }

  onGenerar(): void {
    if (this.form.invalid) return;

    this.houses.listar().subscribe(casas => {
      if (!casas.length) { alert('No se encontró tu casa asociada.'); return; }
      const v = this.form.value;
      const dto = {
        houseId: casas[0].id,
        visitorName: v.visitorName!,
        vehiclePlate: v.vehiclePlate || null,
        notes: v.notes || null
      };
      this.passes.crear(dto).subscribe({
        next: creado => {
          bootstrap.Modal.getInstance(document.getElementById('modalNuevoQr'))?.hide();
          this.qrParaMostrar = { visitorName: creado.visitorName, qrImageBase64: creado.qrImageBase64 };
          new bootstrap.Modal(document.getElementById('modalVerQr')).show();
          this.cargar();
        },
        error: err => alert(err.error?.mensaje || 'No se pudo generar el QR.')
      });
    });
  }

  verQr(pass: AccessPass): void {
    if (!pass.qrImageBase64) return;
    this.qrParaMostrar = { visitorName: pass.visitorName, qrImageBase64: pass.qrImageBase64 };
    new bootstrap.Modal(document.getElementById('modalVerQr')).show();
  }

  revocar(id: string): void {
    if (!confirm('¿Revocar este QR? El visitante ya no podrá usarlo.')) return;
    this.passes.revocar(id).subscribe(() => this.cargar());
  }
}
