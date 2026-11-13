import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ErroApi } from '../../core/erro-api';
import { NotificacaoUiService } from '../../core/notificacao-ui.service';
import { ErroFormularioComponent } from '../../shared/erro-formulario.component';
import { igualA, palavraPasseForte } from '../../shared/validadores';
import { AutenticacaoApiService } from './autenticacao-api.service';

type Campo = 'nome' | 'email' | 'password' | 'confirmacao';

interface ContaCriada {
  email: string;
  mensagem: string;
  envioFalhou: boolean;
}

const REGRA_PASSWORD = 'Usa pelo menos 8 caracteres, com uma letra e um dígito.';

const MENSAGENS: Record<Campo, Record<string, string>> = {
  nome: {
    required: 'Indica o teu nome.',
    pattern: 'Indica o teu nome.',
    maxlength: 'O nome tem no máximo 100 caracteres.',
  },
  email: {
    required: 'Indica o teu email académico.',
    email: 'Indica um email válido.',
  },
  password: {
    required: 'Indica uma palavra-passe.',
    minlength: REGRA_PASSWORD,
    palavraPasseFraca: REGRA_PASSWORD,
  },
  confirmacao: {
    required: 'Repete a palavra-passe.',
    diferente: 'As palavras-passe não coincidem.',
  },
};

@Component({
  selector: 'app-registo-page',
  imports: [ReactiveFormsModule, RouterLink, ErroFormularioComponent],
  template: `<section class="auth-card">
    @if (contaCriada(); as conta) {
      <p class="eyebrow">CONTA CRIADA</p>
      @if (conta.envioFalhou) {
        <h1>Falta a confirmação.</h1>
        <p class="notice error" role="alert">{{ conta.mensagem }}</p>
      } @else {
        <h1>Verifica o teu email.</h1>
        <p class="notice" role="status">{{ conta.mensagem }}</p>
        <p class="muted">Enviámos a ligação para {{ conta.email }}. É válida durante 24 horas.</p>
      }
      <p class="muted">
        Não recebeste a ligação?
        <a routerLink="/reenviar-confirmacao" [state]="{ email: conta.email }">Pede uma nova</a>
      </p>
      <a routerLink="/entrar">Ir para a página de entrada</a>
    } @else {
      <p class="eyebrow">JUNTA-TE À COMUNIDADE</p>
      <h1>Cria a tua conta.</h1>
      <p class="muted">
        Usa o email da tua instituição de ensino. Antes de entrares, terás de o confirmar.
      </p>
      <form
        [formGroup]="formulario"
        (ngSubmit)="registar()"
        [attr.aria-busy]="aRegistar()"
        novalidate
      >
        <label for="nome">Nome</label>
        <input
          id="nome"
          formControlName="nome"
          autocomplete="name"
          [attr.aria-invalid]="invalido('nome')"
          aria-describedby="erro-nome"
        />
        <div id="erro-nome"><app-erro-formulario [erros]="erros('nome')" /></div>
        <label for="email">Email académico</label>
        <input
          id="email"
          type="email"
          formControlName="email"
          autocomplete="email"
          [attr.aria-invalid]="invalido('email')"
          aria-describedby="erro-email"
        />
        <div id="erro-email"><app-erro-formulario [erros]="erros('email')" /></div>
        <label for="password">Palavra-passe</label>
        <input
          id="password"
          type="password"
          formControlName="password"
          autocomplete="new-password"
          [attr.aria-invalid]="invalido('password')"
          aria-describedby="ajuda-password erro-password"
        />
        @if (!invalido('password')) {
          <small id="ajuda-password" class="muted">
            Pelo menos 8 caracteres, com uma letra e um dígito.
          </small>
        }
        <div id="erro-password"><app-erro-formulario [erros]="erros('password')" /></div>
        <label for="confirmacao">Confirmar palavra-passe</label>
        <input
          id="confirmacao"
          type="password"
          formControlName="confirmacao"
          autocomplete="new-password"
          [attr.aria-invalid]="invalido('confirmacao')"
          aria-describedby="erro-confirmacao"
        />
        <div id="erro-confirmacao"><app-erro-formulario [erros]="erros('confirmacao')" /></div>
        @if (erro()) {
          <p class="field-error" role="alert">{{ erro() }}</p>
        }
        <button class="primary" type="submit" [disabled]="aRegistar()">
          {{ aRegistar() ? 'A criar a conta…' : 'Criar conta' }}
        </button>
      </form>
      <p class="muted">Já tens conta? <a routerLink="/entrar">Entra na comunidade</a></p>
      <p class="muted">
        Não recebeste a ligação de confirmação?
        <a routerLink="/reenviar-confirmacao" [state]="{ email: formulario.controls.email.value }"
          >Pede uma nova</a
        >
      </p>
      <a routerLink="/">Voltar ao início</a>
    }
  </section>`,
})
export class RegistoPage {
  private readonly auth = inject(AutenticacaoApiService);
  private readonly ui = inject(NotificacaoUiService);
  private readonly destroyRef = inject(DestroyRef);
  // Validação só para usabilidade: quem decide é a API, e o domínio do email vem da configuração dela.
  readonly formulario = inject(FormBuilder).nonNullable.group({
    nome: ['', [Validators.required, Validators.pattern(/\S/), Validators.maxLength(100)]],
    // O Validators.email já recusa endereços com mais de 254 caracteres.
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8), palavraPasseForte]],
    confirmacao: ['', [Validators.required, igualA('password')]],
  });
  readonly aRegistar = signal(false);
  readonly erro = signal('');
  readonly contaCriada = signal<ContaCriada | null>(null);

  constructor() {
    const { password, confirmacao } = this.formulario.controls;
    password.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => confirmacao.updateValueAndValidity());
  }

  invalido(campo: Campo) {
    const controlo = this.formulario.controls[campo];
    return controlo.touched && controlo.invalid;
  }

  erros(campo: Campo): string[] {
    if (!this.invalido(campo)) return [];
    const [primeiro] = Object.keys(this.formulario.controls[campo].errors ?? {});
    return [MENSAGENS[campo][primeiro]];
  }

  registar() {
    if (this.aRegistar()) return;
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid) return;
    this.ui.limpar();
    this.erro.set('');
    this.aRegistar.set(true);
    const { nome, email, password } = this.formulario.getRawValue();
    const emailLimpo = email.trim();
    this.auth
      .registar({ nome: nome.trim(), email: emailLimpo, password })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.aRegistar.set(false)),
      )
      .subscribe({
        next: ({ mensagem }) => this.concluir(emailLimpo, mensagem, false),
        error: (erro: ErroApi) => {
          // O 503 da API significa que a conta ficou criada e só falhou o email: repetir daria 409.
          // Um 503 sem mensagem da API vem da infraestrutura e não garante que a conta exista.
          if (erro.status === 503 && erro.mensagemDaApi)
            this.concluir(emailLimpo, erro.message, true);
          else this.erro.set(erro.message);
        },
      });
  }

  private concluir(email: string, mensagem: string, envioFalhou: boolean) {
    this.formulario.reset();
    this.contaCriada.set({ email, mensagem, envioFalhou });
  }
}
