import { Component, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Html5Qrcode } from 'html5-qrcode';
import { AccessService } from '../../core/services/access.service';
import { ScanResponse } from '../../core/models/models';

@Component({
  selector: 'app-escanear',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './escanear.component.html'
})
export class EscanearComponent implements OnDestroy {
  camaraActiva = false;
  tokenManual = '';
  resultado: ScanResponse | null = null;
  private lector: Html5Qrcode | null = null;

  // Mientras un QR está en resultado, la cámara queda pausada y se ignora cualquier
  // otro código detectado: sin esto, si el guardia sostiene el QR frente a la cámara
  // unos segundos, el lector lo vuelve a leer varias veces por segundo y el segundo
  // intento sale como "ya usado" aunque el primero sí fue válido.
  private bloqueado = false;

  constructor(private access: AccessService) {}

  iniciarCamara(): void {
    this.resultado = null;
    this.bloqueado = false;
    this.lector = new Html5Qrcode('lectorQr');
    this.lector.start(
      { facingMode: 'environment' },
      { fps: 10, qrbox: 250 },
      (decodedText) => this.onCodigoDetectado(decodedText),
      () => {}
    ).then(() => this.camaraActiva = true)
      .catch(err => alert('No se pudo acceder a la cámara: ' + err));
  }

  detenerCamara(): void {
    if (this.lector) {
      this.lector.stop().then(() => this.lector?.clear()).catch(() => {});
      this.lector = null;
    }
    this.camaraActiva = false;
    this.bloqueado = false;
  }

  private onCodigoDetectado(decodedText: string): void {
    if (this.bloqueado) return;
    this.bloqueado = true;
    this.lector?.pause(true);
    this.escanear(decodedText);
  }

  // Para volver a habilitar la cámara después de mostrar un resultado.
  escanearSiguiente(): void {
    this.resultado = null;
    this.bloqueado = false;
    this.lector?.resume();
  }

  escanearManual(): void {
    if (this.tokenManual.trim()) this.escanear(this.tokenManual.trim());
  }

  private escanear(token: string): void {
    this.access.scan(token).subscribe({
      next: resp => { this.resultado = resp; this.tokenManual = ''; },
      error: err => {
        alert(err.error?.mensaje || 'Error al escanear.');
        this.bloqueado = false;
        this.lector?.resume();
      }
    });
  }

  ngOnDestroy(): void { this.detenerCamara(); }
}
