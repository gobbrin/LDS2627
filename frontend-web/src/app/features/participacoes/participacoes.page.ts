import { TempoRealService } from '../notificacoes/tempo-real.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ApiClient } from '../../core/api-client';
import { SessaoService } from '../../core/sessao.service';
import { PedidoEstado, DataPortugalPipe } from '../../shared/pedido-estado';
import { Detalhe, PesquisaApi } from '../pesquisa/pesquisa-api';
interface Pedido {
  id: string;
  viagemId: string;
  nome?: string;
  origem?: string;
  destino?: string;
  estado: string;
  partidaEm?: string;
  recolha?: string;
}
interface Simulacao {
  pedidoId: string;
  distanciaOriginalKm: number;
  duracaoOriginalMinutos: number;
  distanciaSimuladaKm: number;
  duracaoSimuladaMinutos: number;
  desvioDistanciaKm: number;
  desvioDuracaoMinutos: number;
  ordemOriginal: string[];
  ordemSimulada: string[];
}
@Component({
  imports: [DataPortugalPipe, FormsModule, RouterLink],
  template: `<h1>Participações</h1>
    <button (click)="carregar()" [disabled]="ocupado()">Atualizar</button>
    @if (erro()) {
      <p role="alert">{{ erro() }}</p>
    }
    @if (aviso()) {
      <p role="status">{{ aviso() }}</p>
    }
    @if (viagem(); as v) {
      <h2>{{ v.origem }} → {{ v.destino }}</h2>
      <p>{{ v.lugaresDisponiveis }} lugares disponíveis · {{ v.estado }}</p>
      @if (!condutor()) {
        <form (ngSubmit)="pedir()">
          <label
            >Recolha (opcional)<input name="recolha" [(ngModel)]="recolha" maxlength="200" /></label
          ><label
            >Destino do passageiro (opcional)<input
              name="destino"
              [(ngModel)]="destino"
              maxlength="200"
          /></label>
          <p>
            Sem pontos adicionais, usas a origem e o destino da viagem. O pedido não reserva um
            lugar; aguarda a decisão do condutor.
          </p>
          <button [disabled]="ocupado() || v.lugaresDisponiveis < 1 || v.estado !== 'Ativa'">
            Enviar pedido
          </button>
        </form>
      } @else {
        <h3>Pedidos pendentes</h3>
        @for (p of pendentes(); track p.id) {
          <article class="card">
            <p>{{ p.nome }} · {{ p.recolha || v.origem }} → {{ p.destino || v.destino }}</p>
            <button (click)="simular(p.id)" [disabled]="ocupado()">Simular rota</button
            ><button
              (click)="decidir(p.id, 'aceitar')"
              [disabled]="ocupado() || simulacao()?.pedidoId !== p.id"
            >
              Aceitar após simulação</button
            ><button (click)="decidir(p.id, 'rejeitar')" [disabled]="ocupado()">Rejeitar</button>
          </article>
        } @empty {
          <p>Não existem pedidos pendentes.</p>
        }
        @if (simulacao(); as s) {
          <h3>Simulação antes da decisão</h3>
          <p>
            Original: {{ s.distanciaOriginalKm }} km / {{ s.duracaoOriginalMinutos }} min. Simulada:
            {{ s.distanciaSimuladaKm }} km / {{ s.duracaoSimuladaMinutos }} min.
          </p>
          <p>Desvio: {{ s.desvioDistanciaKm }} km / {{ s.desvioDuracaoMinutos }} min.</p>
          <p>Ordem original: {{ s.ordemOriginal.join(' → ') }}</p>
          <p>Ordem simulada: {{ s.ordemSimulada.join(' → ') }}</p>
          <p>
            A simulação não altera a viagem. A representação geográfica aguarda as coordenadas da
            rota simulada.
          </p>
        }
        <h3>Passageiros aceites</h3>
        @for (p of aceites(); track p.id) {
          <p>{{ p.nome }} · {{ p.recolha || v.origem }} → {{ p.destino || v.destino }}</p>
          <button (click)="decidir(p.id, 'cancelar')" [disabled]="ocupado()">
            Cancelar participação
          </button>
        }
      }
    }
    <h2>Os meus pedidos</h2>
    @for (p of meus(); track p.id) {
      <article class="card">
        <a [routerLink]="['/pesquisa', p.viagemId]">{{ p.origem }} → {{ p.destino }}</a>
        <p>{{ p.estado }} · {{ p.partidaEm | dataPortugal }}</p>
        @if (p.estado === 'Pendente' || p.estado === 'Aceite') {
          <button (click)="decidir(p.id, 'cancelar')" [disabled]="ocupado()">
            Cancelar pedido/participação
          </button>
        }
      </article>
    } @empty {
      <p>Não existem pedidos.</p>
    }`,
})
export class ParticipacoesPage extends PedidoEstado {
  private readonly api = inject(ApiClient);
  private readonly pesquisa = inject(PesquisaApi);
  private readonly sessao = inject(SessaoService);
  readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id');
  readonly viagem = signal<Detalhe | null>(null);
  readonly meus = signal<Pedido[]>([]);
  readonly pendentes = signal<Pedido[]>([]);
  readonly aceites = signal<Pedido[]>([]);
  readonly simulacao = signal<Simulacao | null>(null);
  recolha = '';
  destino = '';
  condutor() {
    return this.viagem()?.condutorId === this.sessao.utilizador()?.id;
  }
  constructor() {
    super();
    this.carregar();
    inject(TempoRealService)
      .alteracoes.pipe(takeUntilDestroyed(this.destruir))
      .subscribe(() => this.carregar());
  }
  carregar() {
    this.simulacao.set(null);
    this.executar(
      this.api.get<{ pedidosEfetuados: Pedido[] }>('historico', undefined, { erroNaPagina: true }),
      (h) => {
        this.meus.set(h.pedidosEfetuados);
        if (this.id) queueMicrotask(() => this.carregarViagem());
      },
    );
  }
  private carregarViagem() {
    this.executar(this.pesquisa.detalhe(this.id!), (v) => {
      this.viagem.set(v);
      if (this.condutor())
        queueMicrotask(() =>
          this.executar(
            forkJoin({
              p: this.api.get<Pedido[]>(`viagens/${this.id}/pedidos`),
              a: this.api.get<Pedido[]>(`viagens/${this.id}/passageiros`),
            }),
            (r) => {
              this.pendentes.set(r.p);
              this.aceites.set(r.a);
            },
          ),
        );
    });
  }
  pedir() {
    if (!this.id || !this.viagem() || this.condutor()) return;
    this.executar(
      this.api.post(
        'pedidos',
        {
          viagemId: this.id,
          recolha: this.recolha.trim() || null,
          destino: this.destino.trim() || null,
        },
        { erroNaPagina: true },
      ),
      () => {
        this.aviso.set('Pedido enviado. Aguarda a decisão do condutor.');
        queueMicrotask(() => this.carregar());
      },
    );
  }
  simular(pedidoId: string) {
    this.simulacao.set(null);
    this.executar(
      this.api.post<Simulacao>(
        `viagens/${this.id}/simulacao`,
        { pedidoId },
        { erroNaPagina: true },
      ),
      (s) => this.simulacao.set(s),
    );
  }
  decidir(id: string, acao: 'aceitar' | 'rejeitar' | 'cancelar') {
    if (acao === 'aceitar' && this.simulacao()?.pedidoId !== id) return;
    if (!confirm(`Confirmas que pretendes ${acao} esta participação?`)) return;
    this.executar(this.api.post(`participacoes/${id}/${acao}`, null, { erroNaPagina: true }), () =>
      queueMicrotask(() => this.carregar()),
    );
  }
}
