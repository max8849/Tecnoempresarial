import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { HouseService } from '../../core/services/house.service';
import { AccessPassService } from '../../core/services/access-pass.service';
import { AccessEventService } from '../../core/services/access-event.service';
import { InventoryService } from '../../core/services/inventory.service';
import { AccessEvent } from '../../core/models/models';
import { etiquetaEvento, etiquetaResultado } from '../../core/utils/etiquetas-acceso';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  totalCasas = 0;
  totalResidentes = 0;
  qrsActivos = 0;
  totalInventario = 0;
  ultimosEventos: AccessEvent[] = [];
  etiquetaEvento = etiquetaEvento;
  etiquetaResultado = etiquetaResultado;

  constructor(
    private houses: HouseService,
    private passes: AccessPassService,
    private events: AccessEventService,
    private inventory: InventoryService,
    public auth: AuthService
  ) {}

  ngOnInit(): void {
    forkJoin({
      casas: this.houses.listar(),
      qrs: this.passes.listar(),
      eventos: this.events.listar(10)
    }).subscribe(({ casas, qrs, eventos }) => {
      this.totalCasas = casas.length;
      this.totalResidentes = casas.reduce((acc, c) => acc + c.totalResidents, 0);
      this.qrsActivos = qrs.filter(p => p.status === 'Active').length;
      this.ultimosEventos = eventos;
    });

    if (this.auth.tieneRol('SUPER_ADMIN')) {
      this.inventory.listar().subscribe(items => this.totalInventario = items.length);
    }
  }
}
