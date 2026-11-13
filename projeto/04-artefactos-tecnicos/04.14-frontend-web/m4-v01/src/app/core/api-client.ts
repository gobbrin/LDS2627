import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { CONFIGURACAO } from './configuracao';
import { ERRO_NA_PAGINA } from './erro-api';

export interface OpcoesPedido {
  /** A página mostra o erro junto do formulário; a notificação global não o repete. */
  erroNaPagina?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly config = inject(CONFIGURACAO);
  get<T>(caminho: string, params?: HttpParams, opcoes?: OpcoesPedido) {
    return this.http.get<T>(this.url(caminho), { params, context: this.contexto(opcoes) });
  }
  post<T>(caminho: string, corpo: unknown = null, opcoes?: OpcoesPedido) {
    return this.http.post<T>(this.url(caminho), corpo, { context: this.contexto(opcoes) });
  }
  put<T>(caminho: string, corpo: unknown, opcoes?: OpcoesPedido) {
    return this.http.put<T>(this.url(caminho), corpo, { context: this.contexto(opcoes) });
  }
  delete<T>(caminho: string, opcoes?: OpcoesPedido) {
    return this.http.delete<T>(this.url(caminho), { context: this.contexto(opcoes) });
  }
  private contexto(opcoes?: OpcoesPedido): HttpContext {
    return new HttpContext().set(ERRO_NA_PAGINA, opcoes?.erroNaPagina ?? false);
  }
  private url(caminho: string): string {
    // O cliente não aceita URLs externas nem caminhos que escapem a /api/v1.
    if (!/^[a-zA-Z0-9][a-zA-Z0-9/_-]*$/.test(caminho))
      throw new Error('Caminho de API inválido. Usa HttpParams para parâmetros.');
    return `${this.config.apiBaseUrl}/${caminho}`;
  }
}
