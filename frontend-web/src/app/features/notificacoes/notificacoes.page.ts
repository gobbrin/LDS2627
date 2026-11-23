import { Component, inject, signal } from '@angular/core';

import { RouterLink } from '@angular/router';
import { HttpParams } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiClient } from '../../core/api-client';
import { PedidoEstado, DataPortugalPipe } from '../../shared/pedido-estado';
import { TempoRealService } from './tempo-real.service';
export interface Notificacao {
  id: string;
  tipo: string;
  titulo: string;
  conteudo: string;
  criadaEm: string;
  lidaEm: string | null;
  viagemId: string | null;
}
export interface Caixa {
  itens: Notificacao[];
  pagina: number;
  totalPaginas: number;
  naoLidas: number;
}
@Component({
  imports: [DataPortugalPipe, RouterLink],
  template: `<h1>Notificações</h1>
    <p role="status">{{ tempo.estado() }}</p>
    @if (erro()) {
      <p role="alert">{{ erro() }}</p>
    }
    <button (click)="carregar()" [disabled]="ocupado()">Atualizar</button
    ><button (click)="lerTodas()" [disabled]="ocupado() || !caixa()?.naoLidas">
      Marcar todas como lidas
    </button>
    @if (caixa(); as c) {
      <p>{{ c.naoLidas }} por ler</p>
      @for (n of c.itens; track n.id) {
        <article class="card">
          <h2>{{ n.titulo }} {{ n.lidaEm ? '' : '· Nova' }}</h2>
          <p>{{ n.conteudo }}</p>
          <small>{{ n.criadaEm | dataPortugal }}</small>
          @if (n.viagemId) {
            <p><a [routerLink]="['/pesquisa', n.viagemId]">Consultar viagem</a></p>
          }
          @if (!n.lidaEm) {
            <button (click)="ler(n.id)" [disabled]="ocupado()">Marcar como lida</button>
          }
        </article>
      } @empty {
        <p>Não existem notificações.</p>
      }
      <button (click)="mudar(-1)" [disabled]="ocupado() || pagina <= 1">Anterior</button
      ><span>Página {{ pagina }} / {{ c.totalPaginas || 1 }}</span
      ><button (click)="mudar(1)" [disabled]="ocupado() || pagina >= c.totalPaginas">
        Seguinte
      </button>
    }`,
})
export class NotificacoesPage extends PedidoEstado {
  private readonly api = inject(ApiClient);
  readonly tempo = inject(TempoRealService);
  readonly caixa = signal<Caixa | null>(null);
  pagina = 1;
  constructor() {
    super();
    this.carregar();
    this.tempo.alteracoes.pipe(takeUntilDestroyed(this.destruir)).subscribe(() => this.carregar());
  }
  carregar() {
    this.executar(
      this.api.get<Caixa>(
        'notificacoes',
        new HttpParams().set('pagina', this.pagina).set('tamanhoPagina', 20),
        { erroNaPagina: true },
      ),
      (c) => this.caixa.set(c),
    );
  }
  mudar(d: number) {
    this.pagina += d;
    this.carregar();
  }
  ler(id: string) {
    this.executar(this.api.put(`notificacoes/${id}/ler`, null), () =>
      queueMicrotask(() => this.carregar()),
    );
  }
  lerTodas() {
    this.executar(this.api.put('notificacoes/ler-todas', null), () =>
      queueMicrotask(() => this.carregar()),
    );
  }
}
