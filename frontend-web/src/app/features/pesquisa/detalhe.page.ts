import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PedidoEstado, DataPortugalPipe } from '../../shared/pedido-estado';
import { PesquisaApi, Detalhe, Rota } from './pesquisa-api';
import { MapaComponent } from './mapa.component';
@Component({
  imports: [DataPortugalPipe, DecimalPipe, RouterLink, MapaComponent],
  template: `<a routerLink="/pesquisa">Voltar à pesquisa</a>
    @if (erro()) {
      <p role="alert">{{ erro() }}</p>
      <button (click)="carregar()" [disabled]="ocupado()">Tentar novamente</button>
    }
    @if (viagem(); as v) {
      <h1>{{ v.origem }} → {{ v.destino }}</h1>
      <p>{{ v.partidaEm | dataPortugal }} · {{ v.estado }}</p>
      <p>
        {{ v.lugaresDisponiveis }} lugares · {{ v.duracaoMinutos ?? 'Duração indisponível' }} min ·
        {{ v.distanciaKm ?? 'Distância indisponível' }} km
      </p>
      <a [routerLink]="['/utilizadores', v.condutorId]"
        >{{ v.condutor.nome }} · {{ v.condutor.classificacaoMedia ?? 'Sem avaliações' }}</a
      >
      @if (v.estimativaCusto; as c) {
        <h2>Custo indicativo</h2>
        <p>
          {{ c.custoPorParticipante | number: '1.2-2' }} {{ c.moeda }} por participante ({{
            c.totalParticipantes
          }}
          participantes). Total: {{ c.custoTotalEstimado | number: '1.2-2' }} {{ c.moeda }}.
        </p>
        <p>A estimativa não é um pagamento nem uma cobrança.</p>
      } @else {
        <p>{{ v.estimativaIndisponivelMotivo || 'Estimativa de custo indisponível.' }}</p>
      }
      @if (rota(); as r) {
        <h2>Pontos da rota</h2>
        <app-mapa [pontos]="r.pontosOrdenados" />
      }
      @if (erroRota()) {
        <p role="status">{{ erroRota() }}</p>
        <button (click)="carregarRota()">Tentar carregar a rota</button>
      }
      <p><a [routerLink]="['/participacoes', id]">Pedir ou gerir participação</a></p>
      <p>
        <a [routerLink]="['/avisos', id]">Avisos da viagem</a> ·
        <a routerLink="/mensagens" [queryParams]="{ destinatario: v.condutorId }"
          >Contactar condutor</a
        >
      </p>
    }`,
})
export class DetalhePage extends PedidoEstado {
  private readonly api = inject(PesquisaApi);
  readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id') ?? '';
  readonly viagem = signal<Detalhe | null>(null);
  readonly rota = signal<Rota | null>(null);
  readonly erroRota = signal('');
  constructor() {
    super();
    this.carregar();
  }
  carregar() {
    this.executar(this.api.detalhe(this.id), (v) => {
      this.viagem.set(v);
      this.carregarRota();
    });
  }
  carregarRota() {
    this.erroRota.set('');
    this.api
      .rota(this.id)
      .pipe(takeUntilDestroyed(this.destruir))
      .subscribe({
        next: (r) => this.rota.set(r),
        error: () => {
          this.rota.set(null);
          this.erroRota.set(
            'A rota está indisponível. Os restantes dados da viagem continuam acessíveis.',
          );
        },
      });
  }
}
