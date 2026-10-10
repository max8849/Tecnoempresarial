import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AccessEvent } from '../models/models';

@Injectable({ providedIn: 'root' })
export class AccessEventService {
  private base = `${environment.apiBaseUrl}/access-events`;

  constructor(private http: HttpClient) {}

  listar(limit = 50) { return this.http.get<AccessEvent[]>(this.base, { params: { limit } }); }
}
