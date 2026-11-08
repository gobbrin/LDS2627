import { Component, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { finalize } from 'rxjs';
import { SessaoService } from './core/sessao.service';
import { NotificacaoUiService } from './core/notificacao-ui.service';
import { AutenticacaoApiService } from './features/autenticacao/autenticacao-api.service';
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `<a class="skip-link" href="#conteudo">Saltar para o conteúdo</a>
    <header>
      <a
        class="brand"
        [routerLink]="sessao.autenticado() ? '/inicio' : '/'"
        aria-label="Campus Carpool — início"
        ><span class="brand-mark" aria-hidden="true">cc</span
        ><span>campus<span class="brand-light">carpool</span></span></a
      ><span class="header-note">O teu campus. A nossa comunidade.</span>
    </header>
    <div class="app-layout" [class.signed-in]="sessao.autenticado()">
      @if (sessao.autenticado()) {
        <nav class="sidebar" aria-label="Navegação principal">
          <p class="eyebrow">A TUA COMUNIDADE</p>
          <a routerLink="/inicio" routerLinkActive="active">Início</a
          ><a routerLink="/perfil" routerLinkActive="active">Perfil</a
          ><button type="button" (click)="sair()" [disabled]="aSair()">
            {{ aSair() ? 'A terminar…' : 'Terminar sessão' }}
          </button>
        </nav>
      }
      <main id="conteudo" tabindex="-1">
        @if (ui.mensagem(); as mensagem) {
          <div
            class="notice"
            [class.error]="mensagem.tipo === 'erro'"
            [attr.role]="mensagem.tipo === 'erro' ? 'alert' : 'status'"
          >
            <span>{{ mensagem.texto }}</span
            ><button type="button" aria-label="Fechar mensagem" (click)="ui.limpar()">×</button>
          </div>
        }
        <router-outlet />
      </main>
    </div>
    <footer><span>Campus Carpool</span><span>Uma comunidade. Muitos caminhos.</span></footer>`,
})
export class App {
  readonly sessao = inject(SessaoService);
  readonly ui = inject(NotificacaoUiService);
  private readonly auth = inject(AutenticacaoApiService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly aSair = signal(false);
  constructor() {
    let tinhaSessao = this.sessao.autenticado();
    effect(() => {
      const temSessao = this.sessao.autenticado();
      if (tinhaSessao && !temSessao) void this.router.navigate(['/entrar']);
      tinhaSessao = temSessao;
    });
    if (this.sessao.obterToken())
      this.auth
        .validar()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          error: () => {
            /* O interceptor trata o erro; só 401 invalida a sessão. */
          },
        });
  }
  sair() {
    if (this.aSair()) return;
    this.aSair.set(true);
    this.auth
      .logout()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.aSair.set(false);
          void this.router.navigate(['/entrar']);
        }),
      )
      .subscribe();
  }
}
