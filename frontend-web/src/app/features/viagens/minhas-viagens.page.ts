import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ErroApi } from '../../core/erro-api';
import { EstadoBadgeComponent } from '../../shared/estado-badge.component';
import { ViagemDoCondutor } from '../../shared/models/viagem';
import { formatarPartida } from './partida';
import { ViagemApiService } from './viagem-api.service';

@Component({
  selector: 'app-minhas-viagens-page',
  imports: [RouterLink, EstadoBadgeComponent],
  template: `<section class="panel">
    <p class="eyebrow">AS TUAS VIAGENS</p>
    <h1>Viagens ativas</h1>
    @if (aCarregar()) {
      <p role="status">A carregar as tuas viagens…</p>
    } @else if (erro()) {
      <p role="alert">{{ erro() }}</p>
      <button type="button" (click)="carregar()">Tentar novamente</button>
    } @else {
      <a class="button primary" routerLink="/viagens/nova">Criar viagem</a>
      @if (viagens().length) {
        <ul class="viagens">
          @for (viagem of viagens(); track viagem.id) {
            <li>
              <div class="viagem-rota">
                <h2>{{ viagem.origem }} <span aria-hidden="true">→</span> {{ viagem.destino }}</h2>
                <app-estado-badge estado="Ativa" />
              </div>
              <p class="muted">
                {{ partida(viagem) }} · {{ viagem.lugaresDisponiveis }} de
                {{ viagem.lugaresOferecidos }} lugares disponíveis
              </p>
              <a
                [routerLink]="['/viagens', viagem.id, 'editar']"
                [attr.aria-label]="'Gerir a viagem de ' + viagem.origem + ' para ' + viagem.destino"
                >Gerir viagem</a
              >
            </li>
          }
        </ul>
      } @else {
        <p class="muted">Ainda não tens viagens ativas.</p>
      }
    }
  </section>`,
})
export class MinhasViagensPage {
  private readonly api = inject(ViagemApiService);
  private readonly destroyRef = inject(DestroyRef);
  readonly viagens = signal<ViagemDoCondutor[]>([]);
  readonly aCarregar = signal(false);
  readonly erro = signal('');

  constructor() {
    this.carregar();
  }

  carregar() {
    if (this.aCarregar()) return;
    this.aCarregar.set(true);
    this.erro.set('');
    this.api
      .listarMinhas()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.aCarregar.set(false)),
      )
      .subscribe({
        next: (viagens) => this.viagens.set(viagens),
        error: (erro: ErroApi) => this.erro.set(erro.message),
      });
  }

  partida(viagem: ViagemDoCondutor) {
    return formatarPartida(viagem.partidaEm);
  }
}
