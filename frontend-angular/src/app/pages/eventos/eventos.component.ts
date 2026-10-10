import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AccessEventService } from '../../core/services/access-event.service';
import { AccessEvent } from '../../core/models/models';
import { etiquetaEvento, etiquetaResultado } from '../../core/utils/etiquetas-acceso';

@Component({
  selector: 'app-eventos',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './eventos.component.html'
})
export class EventosComponent implements OnInit {
  eventos: AccessEvent[] = [];
  etiquetaEvento = etiquetaEvento;
  etiquetaResultado = etiquetaResultado;

  constructor(private service: AccessEventService) {}

  ngOnInit(): void { this.service.listar(100).subscribe(e => this.eventos = e); }
}
