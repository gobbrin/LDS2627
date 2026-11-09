import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DestroyRef } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AutenticacaoApiService } from './autenticacao-api.service';
import { NotificacaoUiService } from '../../core/notificacao-ui.service';
import { ErroApi } from '../../core/erro-api';
import { destinoInterno } from '../../core/guards';
import { ErroFormularioComponent } from '../../shared/erro-formulario.component';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, RouterLink, ErroFormularioComponent],
  template: ` <section class="auth-card">
    <p class="eyebrow">BEM-VINDO DE VOLTA</p>
    <h1>Entra na tua comunidade.</h1>
    <p class="muted">Usa a tua conta académica. Para entrar, o email tem de estar confirmado.</p>
    <form [formGroup]="formulario" (ngSubmit)="entrar()" [attr.aria-busy]="aEntrar()" novalidate>
      <label for="email">Email académico</label
      ><input
        id="email"
        type="email"
        formControlName="email"
        autocomplete="username"
        [attr.aria-invalid]="formulario.controls.email.touched && formulario.controls.email.invalid"
        aria-describedby="erro-email"
      />
      <div id="erro-email"><app-erro-formulario [erros]="errosEmail()" /></div>
      <label for="password">Palavra-passe</label
      ><input
        id="password"
        type="password"
        formControlName="password"
        autocomplete="current-password"
        [attr.aria-invalid]="
          formulario.controls.password.touched && formulario.controls.password.invalid
        "
        aria-describedby="erro-password"
      />
      <div id="erro-password"><app-erro-formulario [erros]="errosPassword()" /></div>
      @if (erro()) {
        <p class="field-error" role="alert">{{ erro() }}</p>
      }
      <button class="primary" type="submit" [disabled]="aEntrar()">
        {{ aEntrar() ? 'A entrar…' : 'Entrar' }}
      </button>
    </form>
    <a routerLink="/">Voltar ao início</a>
  </section>`,
})
export class LoginPage {
  private readonly auth = inject(AutenticacaoApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly ui = inject(NotificacaoUiService);
  private readonly destroyRef = inject(DestroyRef);
  readonly formulario = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });
  readonly aEntrar = signal(false);
  readonly erro = signal('');
  readonly campos = signal<Record<string, string[]>>({});
  errosEmail() {
    return (
      this.campos()['email'] ??
      this.campos()['Email'] ??
      (this.formulario.controls.email.touched && this.formulario.controls.email.invalid
        ? ['Introduz um email válido.']
        : [])
    );
  }
  errosPassword() {
    return (
      this.campos()['password'] ??
      this.campos()['Password'] ??
      (this.formulario.controls.password.touched && this.formulario.controls.password.invalid
        ? ['Introduz a palavra-passe.']
        : [])
    );
  }
  entrar() {
    if (this.aEntrar()) return;
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid) return;
    this.ui.limpar();
    this.erro.set('');
    this.campos.set({});
    this.aEntrar.set(true);
    const dados = this.formulario.getRawValue();
    this.auth
      .login({ ...dados, email: dados.email.trim() })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.aEntrar.set(false)),
      )
      .subscribe({
        next: () => {
          this.formulario.controls.password.reset();
          void this.router.navigateByUrl(
            destinoInterno(this.route.snapshot.queryParamMap.get('regresso')),
          );
        },
        error: (erro: ErroApi) => {
          this.erro.set(erro.message);
          this.campos.set(erro.campos);
          this.formulario.controls.password.reset();
        },
      });
  }
}
