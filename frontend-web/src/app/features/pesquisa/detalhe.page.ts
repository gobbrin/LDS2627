import { Component, DestroyRef, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ErroApi } from '../../core/erro-api';
import { formatarPartida, formatarRota } from '../viagens/partida';
import { MapaComponent } from './mapa.component';
import { DetalheViagem, PesquisaApi, RotaDaViagem, textoClassificacao } from './pesquisa-api';

/**
 * Detalhe de uma viagem (F003 e F008): dados públicos do condutor, lugares, distância, duração,
 * custo indicativo e a rota no mapa. A rota carrega à parte: se o serviço de mapas falhar, o
 * resto do detalhe continua visível e a falha é comunicada.
 */
@Component({
  selector: 'app-detalhe-viagem-page',
  imports: [DecimalPipe, RouterLink, MapaComponent],
  template: `<section class="panel">
    <a routerLink="/pesquisa">Voltar à pesquisa</a>
    @if (aCarregar()) {
      <p role="status">A carregar a viagem…</p>
    } @else if (erro()) {
      <p role="alert">{{ erro() }}</p>
      <button type="button" (click)="carregar()">Tentar novamente</button>
    } @else if (viagem(); as v) {
      <h1>{{ v.origem }} <span aria-hidden="true">→</span> {{ v.destino }}</h1>
      <p>{{ partida(v) }} · {{ v.estado }}</p>
      <p>
        {{ v.lugaresDisponiveis }} lugares disponíveis · {{ v.pontosAdicionais }} pontos adicionais
      </p>
      <p>{{ rotaResumida(v) || 'Distância e duração indisponíveis.' }}</p>
      <p>
        Condutor:
        @if (v.condutor; as condutor) {
          <a [routerLink]="['/utilizadores', v.condutorId]">{{ condutor.nome }}</a>
          ·
          {{ classificacao(condutor) }}
        } @else {
          indisponível
        }
      </p>

      <h2>Custo indicativo</h2>
      @if (v.estimativaCusto; as custo) {
        <p>
          {{ custo.custoPorParticipante | number: '1.2-2' }} {{ custo.moeda }} por participante ({{
            custo.totalParticipantes
          }}
          participantes); total {{ custo.custoTotalEstimado | number: '1.2-2' }} {{ custo.moeda }}.
        </p>
        <p class="muted">
          Valor indicativo para dividir despesas: não é um preço nem um pagamento.
        </p>
      } @else {
        <p>{{ v.estimativaIndisponivelMotivo || 'Estimativa de custo indisponível.' }}</p>
      }

      <h2>Rota</h2>
      @if (aCarregarRota()) {
        <p role="status">A carregar a rota…</p>
      } @else if (erroRota()) {
        <p role="alert">{{ erroRota() }}</p>
        <button type="button" (click)="carregarRota()">Tentar carregar a rota</button>
      } @else if (rota(); as r) {
        <p>{{ totaisDaRota(r) }}</p>
        <app-mapa
          [pontos]="r.pontosOrdenados"
          [geometria]="r.geometria"
          [aproximada]="r.geometriaAproximada"
        />
      }

      <p><a [routerLink]="['/participacoes', id]">Pedir ou gerir participação</a></p>
      <p>
        <a [routerLink]="['/avisos', id]">Avisos da viagem</a> ·
        <a routerLink="/mensagens" [queryParams]="{ destinatario: v.condutorId }">Contactar condutor</a>
      </p>
    }
  </section>`,
})
export class DetalhePage {
  private readonly api = inject(PesquisaApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id') ?? '';

  readonly viagem = signal<DetalheViagem | null>(null);
  readonly aCarregar = signal(false);
  readonly erro = signal('');
  readonly rota = signal<RotaDaViagem | null>(null);
  readonly aCarregarRota = signal(false);
  readonly erroRota = signal('');

  constructor() {
    this.carregar();
  }

  /** Carrega o detalhe e, se correr bem, a rota. */
  carregar() {
    if (this.aCarregar()) return;
    this.aCarregar.set(true);
    this.erro.set('');
    this.api
      .detalhe(this.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.aCarregar.set(false)),
      )
      .subscribe({
        next: (viagem) => {
          this.viagem.set(viagem);
          this.carregarRota();
        },
        error: (erro: ErroApi) => this.erro.set(erro.message),
      });
  }

  /** Carrega a rota; uma falha (por exemplo 502/503 do serviço de mapas) só afeta esta parte. */
  carregarRota() {
    if (this.aCarregarRota()) return;
    this.aCarregarRota.set(true);
    this.erroRota.set('');
    this.api
      .rota(this.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.aCarregarRota.set(false)),
      )
      .subscribe({
        next: (rota) => this.rota.set(rota),
        error: (erro: ErroApi) => {
          this.rota.set(null);
          this.erroRota.set(`A rota está indisponível: ${erro.message}`);
        },
      });
  }

  readonly classificacao = textoClassificacao;

  partida(viagem: DetalheViagem) {
    return formatarPartida(viagem.partidaEm);
  }

  rotaResumida(viagem: DetalheViagem) {
    return formatarRota(viagem.distanciaKm, viagem.duracaoMinutos);
  }

  /** Distância e duração totais da rota com os pontos que o utilizador vê. */
  totaisDaRota(rota: RotaDaViagem) {
    return formatarRota(rota.distanciaTotalKm, rota.duracaoTotalMinutos);
  }
}
