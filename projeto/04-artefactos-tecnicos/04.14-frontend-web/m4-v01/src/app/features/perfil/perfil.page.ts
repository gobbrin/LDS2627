import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { PerfilApiService } from './perfil-api.service';
import { PerfilProprio } from '../../shared/models/perfil';

@Component({
  selector: 'app-perfil-page',
  template: `<section class="panel">
    <p class="eyebrow">A TUA CONTA</p>
    <h1>Perfil</h1>
    @if (aCarregar()) {
      <p role="status">A carregar o perfil…</p>
    } @else if (perfil(); as p) {
      <div class="profile-heading">
        <span class="avatar" aria-hidden="true">{{ p.nome.charAt(0) }}</span>
        <div>
          <h2>{{ p.nome }}</h2>
          <p>{{ p.email }}</p>
        </div>
      </div>
      <dl>
        <div>
          <dt>Curso</dt>
          <dd>{{ p.curso || 'Não indicado' }}</dd>
        </div>
        <div>
          <dt>Instituição</dt>
          <dd>{{ p.instituicao || 'Não indicada' }}</dd>
        </div>
        <div>
          <dt>Avaliações</dt>
          <dd>
            {{ p.classificacaoMedia === null ? 'Sem avaliações' : p.classificacaoMedia + ' / 5' }}
          </dd>
        </div>
      </dl>
    } @else {
      <p role="alert">Não foi possível carregar o perfil.</p>
      <button type="button" (click)="carregar()">Tentar novamente</button>
    }
  </section>`,
})
export class PerfilPage {
  private readonly api = inject(PerfilApiService);
  private readonly destroyRef = inject(DestroyRef);
  readonly perfil = signal<PerfilProprio | null>(null);
  readonly aCarregar = signal(false);
  constructor() {
    this.carregar();
  }
  carregar() {
    if (this.aCarregar()) return;
    this.aCarregar.set(true);
    this.api
      .obterProprio()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.aCarregar.set(false)),
      )
      .subscribe({ next: (p) => this.perfil.set(p), error: () => this.perfil.set(null) });
  }
}
