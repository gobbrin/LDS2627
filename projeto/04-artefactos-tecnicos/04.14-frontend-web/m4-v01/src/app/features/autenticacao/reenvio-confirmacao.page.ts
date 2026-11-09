import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ErroApi } from '../../core/erro-api';
import { NotificacaoUiService } from '../../core/notificacao-ui.service';
import { ErroFormularioComponent } from '../../shared/erro-formulario.component';
import { AutenticacaoApiService } from './autenticacao-api.service';

@Component({
  selector: 'app-reenvio-confirmacao-page',
  imports: [ReactiveFormsModule, RouterLink, ErroFormularioComponent],
  template: `<section class="auth-card">
    <p class="eyebrow">CONFIRMAÇÃO DE CONTA</p>
    @if (resposta(); as mensagem) {
      <h1>Pedido recebido.</h1>
      <p class="notice" role="status">{{ mensagem }}</p>
      <p class="muted">
        Só a ligação mais recente é válida, durante 24 horas. Podes pedir uma nova ligação por
        minuto.
      </p>
      <a routerLink="/entrar">Ir para a página de entrada</a>
    } @else {
      <h1>Pede uma nova ligação.</h1>
      <p class="muted">Indica o email académico com que criaste a conta.</p>
      <form [formGroup]="formulario" (ngSubmit)="pedir()" [attr.aria-busy]="aEnviar()" novalidate>
        <label for="email">Email académico</label>
        <input
          id="email"
          type="email"
          formControlName="email"
          autocomplete="email"
          [attr.aria-invalid]="invalido()"
          aria-describedby="erro-email"
        />
        <div id="erro-email"><app-erro-formulario [erros]="errosEmail()" /></div>
        @if (erro()) {
          <p class="field-error" role="alert">{{ erro() }}</p>
        }
        <button class="primary" type="submit" [disabled]="aEnviar()">
          {{ aEnviar() ? 'A enviar…' : 'Enviar nova ligação' }}
        </button>
      </form>
      <a routerLink="/entrar">Voltar à página de entrada</a>
    }
  </section>`,
})
export class ReenvioConfirmacaoPage {
  private readonly auth = inject(AutenticacaoApiService);
  private readonly ui = inject(NotificacaoUiService);
  private readonly destroyRef = inject(DestroyRef);
  // O email vem no estado da navegação, para não ficar no URL nem no histórico do browser.
  private readonly emailConhecido: unknown =
    inject(Router).currentNavigation()?.extras.state?.['email'];
  // Validação no cliente necessária: a API responde 202 a um email malformado (DEF-BE-045).
  readonly formulario = inject(FormBuilder).nonNullable.group({
    email: [
      typeof this.emailConhecido === 'string' ? this.emailConhecido : '',
      [Validators.required, Validators.email],
    ],
  });
  readonly aEnviar = signal(false);
  readonly erro = signal('');
  readonly resposta = signal('');

  invalido() {
    const { touched, invalid } = this.formulario.controls.email;
    return touched && invalid;
  }

  errosEmail(): string[] {
    if (!this.invalido()) return [];
    return [
      this.formulario.controls.email.hasError('required')
        ? 'Indica o teu email académico.'
        : 'Indica um email válido.',
    ];
  }

  pedir() {
    if (this.aEnviar()) return;
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid) return;
    this.ui.limpar();
    this.erro.set('');
    this.aEnviar.set(true);
    this.auth
      .reenviarConfirmacao(this.formulario.getRawValue().email)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.aEnviar.set(false)),
      )
      .subscribe({
        next: ({ mensagem }) => this.resposta.set(mensagem),
        error: (erro: ErroApi) => this.erro.set(erro.message),
      });
  }
}
