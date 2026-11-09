import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ErroApi } from '../../core/erro-api';
import { NotificacaoUiService } from '../../core/notificacao-ui.service';
import { AutenticacaoApiService } from './autenticacao-api.service';

interface Falha {
  mensagem: string;
  definitiva: boolean;
}

@Component({
  selector: 'app-confirmacao-page',
  imports: [RouterLink],
  template: `<section class="auth-card">
    <p class="eyebrow">CONFIRMAÇÃO DE CONTA</p>
    @if (falha(); as motivo) {
      @if (motivo.definitiva) {
        <h1>Esta ligação não pode ser usada.</h1>
        <p class="notice error" role="alert">{{ motivo.mensagem }}</p>
        <div class="hero-actions">
          <a class="button primary" routerLink="/reenviar-confirmacao">Pedir nova ligação</a>
          <a class="button" routerLink="/entrar">Ir para a entrada</a>
        </div>
      } @else {
        <h1>Não foi possível confirmar agora.</h1>
        <p class="notice error" role="alert">{{ motivo.mensagem }}</p>
        <button class="primary" type="button" (click)="confirmar()">Tentar novamente</button>
      }
    } @else {
      <h1>A confirmar a tua conta…</h1>
      <p class="muted" role="status">Aguarda um momento.</p>
    }
  </section>`,
})
export class ConfirmacaoPage {
  private readonly auth = inject(AutenticacaoApiService);
  private readonly router = inject(Router);
  private readonly ui = inject(NotificacaoUiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly token = inject(ActivatedRoute).snapshot.queryParamMap.get('token')?.trim();
  readonly falha = signal<Falha | null>(null);

  constructor() {
    this.confirmar();
  }

  confirmar() {
    if (!this.token) {
      this.falha.set({
        mensagem: 'Esta ligação de confirmação não é válida. Pede uma nova ligação.',
        definitiva: true,
      });
      return;
    }
    this.falha.set(null);
    this.auth
      .confirmar(this.token)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.ui.sucesso('Conta confirmada. Já podes entrar.', { manterAoNavegar: true });
          // Sem substituir o histórico, voltar atrás reabria a ligação e dizia que já foi usada.
          void this.router.navigateByUrl('/entrar', { replaceUrl: true });
        },
        // A API só distingue expirada, usada e inválida no texto: por isso se oferecem os dois caminhos.
        // Nas restantes falhas o token não foi consumido e a mesma ligação continua a servir.
        error: (erro: ErroApi) =>
          this.falha.set({
            mensagem: erro.message,
            definitiva: [400, 404].includes(erro.status),
          }),
      });
  }
}
