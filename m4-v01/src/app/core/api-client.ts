import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { CONFIGURACAO } from './configuracao';

@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly config = inject(CONFIGURACAO);
  get<T>(caminho: string, params?: HttpParams) {
    return this.http.get<T>(this.url(caminho), { params });
  }
  post<T>(caminho: string, corpo: unknown = null) {
    return this.http.post<T>(this.url(caminho), corpo);
  }
  put<T>(caminho: string, corpo: unknown) {
    return this.http.put<T>(this.url(caminho), corpo);
  }
  delete<T>(caminho: string) {
    return this.http.delete<T>(this.url(caminho));
  }
  private url(caminho: string): string {
    // O cliente não aceita URLs externas nem caminhos que escapem a /api/v1.
    if (!/^[a-zA-Z0-9][a-zA-Z0-9/_-]*$/.test(caminho))
      throw new Error('Caminho de API inválido. Usa HttpParams para parâmetros.');
    return `${this.config.apiBaseUrl}/${caminho}`;
  }
}
