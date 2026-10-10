import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ScanResponse, ValidateTokenResponse } from '../models/models';

@Injectable({ providedIn: 'root' })
export class AccessService {
  private base = `${environment.apiBaseUrl}/access`;

  constructor(private http: HttpClient) {}

  validate(token: string) { return this.http.post<ValidateTokenResponse>(`${this.base}/validate`, { token }); }

  scan(token: string, accessPointId: string | null = null) {
    return this.http.post<ScanResponse>(`${this.base}/scan`, { token, accessPointId });
  }
}
