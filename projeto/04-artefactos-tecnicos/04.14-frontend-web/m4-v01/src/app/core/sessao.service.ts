import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { CONFIGURACAO } from './configuracao';
import { Sessao, TokenAcesso } from '../shared/models/sessao';

function lerToken(token: string): { sub: string; exp: number } {
  const partes = token.split('.');
  if (partes.length !== 3) throw new Error('Token inválido.');
  const p = JSON.parse(atob(partes[1].replace(/-/g, '+').replace(/_/g, '/')));
  if (typeof p.sub !== 'string' || !p.sub || !Number.isFinite(p.exp))
    throw new Error('Token inválido.');
  return p;
}

@Injectable({ providedIn: 'root' })
export class SessaoService {
  // A configuração faz parte da chave: mudar de backend não reutiliza a sessão anterior.
  private readonly chave = `campus-carpool:sessao:${inject(CONFIGURACAO).apiBaseUrl}`;
  private readonly estado = signal<Sessao | null>(null);
  private temporizador?: ReturnType<typeof setTimeout>;
  readonly utilizador = computed(() => this.estado()?.utilizador ?? null);
  readonly autenticado = computed(() => this.estado() !== null);

  constructor() {
    inject(DestroyRef).onDestroy(() => clearTimeout(this.temporizador));
    try {
      const raw = sessionStorage.getItem(this.chave);
      if (raw) {
        const sessao = JSON.parse(raw) as Sessao;
        const token = lerToken(sessao.accessToken);
        if (
          !Number.isFinite(sessao.expiraEm) ||
          sessao.expiraEm > token.exp * 1000 ||
          sessao.utilizador?.id !== token.sub ||
          sessao.expiraEm <= Date.now()
        )
          throw new Error('Sessão inválida.');
        this.definir(sessao);
      }
    } catch {
      this.terminar();
    }
  }

  iniciar(resposta: TokenAcesso): void {
    const token = lerToken(resposta.accessToken);
    if (!Number.isFinite(resposta.expiresIn) || resposta.expiresIn <= 0)
      throw new Error('Validade inválida.');
    const expiraEm = Math.min(Date.now() + resposta.expiresIn * 1000, token.exp * 1000);
    if (expiraEm <= Date.now()) throw new Error('Token expirado.');
    const sessao: Sessao = {
      accessToken: resposta.accessToken,
      expiraEm,
      utilizador: { id: token.sub },
    };
    // Se o browser bloquear o armazenamento, não anuncia uma sessão persistida.
    sessionStorage.setItem(this.chave, JSON.stringify(sessao));
    this.definir(sessao);
  }

  obterToken(): string | null {
    if (this.expirou()) this.terminar();
    return this.estado()?.accessToken ?? null;
  }

  expirou(): boolean {
    return !this.estado() || this.estado()!.expiraEm <= Date.now();
  }

  terminar(): void {
    clearTimeout(this.temporizador);
    this.estado.set(null);
    try {
      sessionStorage.removeItem(this.chave);
    } catch {
      /* O estado em memória é sempre limpo. */
    }
  }

  private definir(sessao: Sessao): void {
    clearTimeout(this.temporizador);
    this.estado.set(sessao);
    this.temporizador = setTimeout(
      () => {
        if (this.expirou()) this.terminar();
        else this.definir(sessao);
      },
      Math.min(sessao.expiraEm - Date.now(), 2_147_483_647),
    );
  }
}
