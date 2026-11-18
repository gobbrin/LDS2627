import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ErroApi } from '../../core/erro-api';
import { formatarPartida, formatarRota } from '../viagens/partida';
import {
  FiltroPesquisa,
  PesquisaApi,
  TAMANHO_PAGINA,
  ViagemEncontrada,
  textoClassificacao,
} from './pesquisa-api';

/**
 * Pesquisa de viagens (F003): filtros por origem, destino, data e hora mínima, resultados
 * paginados e ligação para o detalhe de cada viagem.
 */
@Component({
  selector: 'app-pesquisa-page',
  imports: [FormsModule, RouterLink],
  template: `<section class="panel">
    <h1>Encontrar viagem</h1>
    <form (ngSubmit)="pesquisar()">
      <label
        >Origem<input id="origem" name="origem" [(ngModel)]="filtro.origem" maxlength="200"
      /></label>
      <label
        >Destino<input id="destino" name="destino" [(ngModel)]="filtro.destino" maxlength="200"
      /></label>
      <label>Data<input id="data" name="data" type="date" [(ngModel)]="filtro.data" /></label>
      <label
        >A partir das<input id="hora" name="hora" type="time" [(ngModel)]="filtro.hora"
      /></label>
      <button type="submit" [disabled]="aCarregar()">
        {{ aCarregar() ? 'A pesquisar…' : 'Pesquisar' }}
      </button>
    </form>

    @if (aCarregar()) {
      <p role="status">A pesquisar viagens…</p>
    } @else if (erro()) {
      <p role="alert">{{ erro() }}</p>
    } @else if (pesquisou()) {
      @if (viagens().length) {
        <p role="status">{{ total() }} viagens encontradas</p>
        <ul class="viagens">
          @for (viagem of viagens(); track viagem.id) {
            <li>
              <h2>
                <a [routerLink]="['/pesquisa', viagem.id]"
                  >{{ viagem.origem }} <span aria-hidden="true">→</span> {{ viagem.destino }}</a
                >
              </h2>
              <p class="muted">
                {{ partida(viagem) }} · {{ viagem.lugaresDisponiveis }} lugares disponíveis
              </p>
              <p class="muted">
                Condutor: {{ viagem.condutor?.nome ?? 'indisponível' }} ·
                {{ classificacao(viagem.condutor) }}
              </p>
              @if (rota(viagem); as texto) {
                <p class="muted">{{ texto }}</p>
              }
            </li>
          }
        </ul>
        @if (total() > tamanhoPagina) {
          <nav aria-label="Paginação">
            <button type="button" (click)="irParaPagina(pagina() - 1)" [disabled]="pagina() <= 1">
              Anterior
            </button>
            <span>Página {{ pagina() }}</span>
            <button
              type="button"
              (click)="irParaPagina(pagina() + 1)"
              [disabled]="pagina() * tamanhoPagina >= total()"
            >
              Seguinte
            </button>
          </nav>
        }
      } @else {
        <p role="status">Nenhuma viagem corresponde aos filtros.</p>
      }
    }
  </section>`,
})
export class PesquisaPage {
  private readonly api = inject(PesquisaApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly tamanhoPagina = TAMANHO_PAGINA;
  readonly classificacao = textoClassificacao;

  /** Filtros escritos no formulário. */
  readonly filtro: FiltroPesquisa = { origem: '', destino: '', data: '', hora: '' };
  /** Filtros da última pesquisa: a paginação usa estes, não o que está a meio de ser escrito. */
  private filtroAplicado: FiltroPesquisa = { ...this.filtro };

  readonly viagens = signal<ViagemEncontrada[]>([]);
  readonly total = signal(0);
  readonly pagina = signal(1);
  readonly aCarregar = signal(false);
  readonly erro = signal('');
  readonly pesquisou = signal(false);

  /** Submissão do formulário: aplica os filtros e volta à primeira página. */
  pesquisar() {
    this.filtroAplicado = { ...this.filtro };
    this.irParaPagina(1);
  }

  /** Pede uma página de resultados com os filtros aplicados. */
  irParaPagina(pagina: number) {
    if (this.aCarregar()) return;
    this.aCarregar.set(true);
    this.erro.set('');
    this.api
      .pesquisar(this.filtroAplicado, pagina)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.aCarregar.set(false)),
      )
      .subscribe({
        next: (resultado) => {
          this.viagens.set(resultado.viagens);
          this.total.set(resultado.total);
          this.pagina.set(pagina);
          this.pesquisou.set(true);
        },
        error: (erro: ErroApi) => this.erro.set(erro.message),
      });
  }

  partida(viagem: ViagemEncontrada) {
    return formatarPartida(viagem.partidaEm);
  }

  rota(viagem: ViagemEncontrada) {
    return formatarRota(viagem.distanciaKm, viagem.duracaoMinutos);
  }
}
