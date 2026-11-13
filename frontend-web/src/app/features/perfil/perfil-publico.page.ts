import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { ErroApi } from '../../core/erro-api';
import { PerfilPublico } from '../../shared/models/perfil';
import { DadosPerfilComponent } from './dados-perfil.component';
import { PerfilApiService } from './perfil-api.service';

@Component({
  selector: 'app-perfil-publico-page',
  imports: [RouterLink, DadosPerfilComponent],
  template: `<section class="panel">
    <p class="eyebrow">A TUA COMUNIDADE</p>
    <h1>Perfil de utilizador</h1>
    @if (aCarregar()) {
      <p role="status">A carregar o perfil…</p>
    } @else if (perfil(); as p) {
      <div class="profile-heading">
        <span class="avatar" aria-hidden="true">{{ p.nome.charAt(0) }}</span>
        <div>
          <h2>{{ p.nome }}</h2>
        </div>
      </div>
      <app-dados-perfil [perfil]="p" />
    } @else {
      <p role="alert">{{ erro() }}</p>
      @if (naoEncontrado()) {
        <a class="button" routerLink="/inicio">Voltar ao início</a>
      } @else {
        <button type="button" (click)="carregar()">Tentar novamente</button>
      }
    }
  </section>`,
})
export class PerfilPublicoPage {
  private readonly api = inject(PerfilApiService);
  private readonly destroyRef = inject(DestroyRef);
  private id = '';
  private pedido?: Subscription;
  readonly perfil = signal<PerfilPublico | null>(null);
  readonly aCarregar = signal(true);
  readonly erro = signal('');
  readonly naoEncontrado = signal(false);

  constructor() {
    // O Angular reutiliza a página quando só muda o :id.
    inject(ActivatedRoute)
      .paramMap.pipe(takeUntilDestroyed())
      .subscribe((parametros) => {
        this.id = parametros.get('id') ?? '';
        this.carregar();
      });
  }

  carregar() {
    this.pedido?.unsubscribe();
    this.perfil.set(null);
    this.naoEncontrado.set(false);
    this.aCarregar.set(true);
    this.pedido = this.api
      .obterPublico(this.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (p) => {
          this.perfil.set(p);
          this.aCarregar.set(false);
        },
        error: (erro: ErroApi) => {
          // A API responde 404 sem corpo a um identificador mal formado (UC-F001-10 E1).
          this.naoEncontrado.set(erro.status === 404);
          this.erro.set(erro.status === 404 ? 'Perfil não encontrado.' : erro.message);
          this.aCarregar.set(false);
        },
      });
  }
}
