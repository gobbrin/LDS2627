import { Component, DestroyRef, inject, Injector, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ErroApi } from '../../core/erro-api';
import { NotificacaoUiService } from '../../core/notificacao-ui.service';
import { ErroFormularioComponent } from '../../shared/erro-formulario.component';
import { focarDepoisDeDesenhar } from '../../shared/focar';
import { LigacaoPerfil, PerfilProprio } from '../../shared/models/perfil';
import { DadosPerfilComponent } from './dados-perfil.component';
import { LigacoesPerfilComponent } from './ligacoes-perfil.component';
import { PerfilApiService } from './perfil-api.service';

type Campo = 'nome' | 'curso' | 'instituicao';

const MENSAGENS: Record<Campo, Record<string, string>> = {
  nome: {
    required: 'Indica o teu nome.',
    pattern: 'Indica o teu nome.',
    maxlength: 'O nome tem no máximo 100 caracteres.',
  },
  curso: { maxlength: 'O curso tem no máximo 150 caracteres.' },
  instituicao: { maxlength: 'A instituição tem no máximo 150 caracteres.' },
};

@Component({
  selector: 'app-perfil-page',
  imports: [
    ReactiveFormsModule,
    DadosPerfilComponent,
    ErroFormularioComponent,
    LigacoesPerfilComponent,
  ],
  template: `<section class="panel">
    <p class="eyebrow">A TUA CONTA</p>
    <h1>{{ aEditar() ? 'Editar perfil' : 'Perfil' }}</h1>
    @if (aCarregar()) {
      <p role="status">A carregar o perfil…</p>
    } @else if (perfil(); as p) {
      @if (aEditar()) {
        <form
          [formGroup]="formulario"
          (ngSubmit)="guardar()"
          [attr.aria-busy]="aGuardar()"
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
          <label for="curso">Curso <span class="muted">(opcional)</span></label>
          <input
            id="curso"
            formControlName="curso"
            autocomplete="off"
            [attr.aria-invalid]="invalido('curso')"
            aria-describedby="erro-curso"
          />
          <div id="erro-curso"><app-erro-formulario [erros]="erros('curso')" /></div>
          <label for="instituicao">Instituição <span class="muted">(opcional)</span></label>
          <input
            id="instituicao"
            formControlName="instituicao"
            autocomplete="organization"
            [attr.aria-invalid]="invalido('instituicao')"
            aria-describedby="erro-instituicao"
          />
          <div id="erro-instituicao"><app-erro-formulario [erros]="erros('instituicao')" /></div>
          @if (erro()) {
            <p class="field-error" role="alert">{{ erro() }}</p>
          }
          <div class="acoes">
            <button class="primary" type="submit" [disabled]="aGuardar()">
              {{ aGuardar() ? 'A guardar…' : 'Guardar alterações' }}
            </button>
            <button type="button" [disabled]="aGuardar()" (click)="fecharEdicao()">Cancelar</button>
          </div>
        </form>
        <app-ligacoes-perfil
          [ligacoes]="p.ligacoesExternas"
          (alteradas)="definirLigacoes($event)"
        />
        <button type="button" [disabled]="aGuardar()" (click)="fecharEdicao()">
          Voltar ao perfil
        </button>
      } @else {
        <div class="profile-heading">
          <span class="avatar" aria-hidden="true">{{ p.nome.charAt(0) }}</span>
          <div>
            <h2>{{ p.nome }}</h2>
            <p>{{ p.email }}</p>
          </div>
        </div>
        <app-dados-perfil [perfil]="p" />
        <button id="editar-perfil" class="primary" type="button" (click)="editar(p)">
          Editar perfil
        </button>
      }
    } @else {
      <p role="alert">{{ erroCarregar() }}</p>
      <button type="button" (click)="carregar()">Tentar novamente</button>
    }
  </section>`,
})
export class PerfilPage {
  private readonly api = inject(PerfilApiService);
  private readonly ui = inject(NotificacaoUiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  readonly perfil = signal<PerfilProprio | null>(null);
  readonly aCarregar = signal(false);
  readonly erroCarregar = signal('');
  readonly aEditar = signal(false);
  readonly aGuardar = signal(false);
  readonly erro = signal('');
  // Validação só para usabilidade: quem decide é a API (P-26).
  readonly formulario = inject(FormBuilder).nonNullable.group({
    nome: ['', [Validators.required, Validators.pattern(/\S/), Validators.maxLength(100)]],
    curso: ['', Validators.maxLength(150)],
    instituicao: ['', Validators.maxLength(150)],
  });

  constructor() {
    this.carregar();
  }

  carregar() {
    if (this.aCarregar()) return;
    this.aCarregar.set(true);
    this.erroCarregar.set('');
    this.api
      .obterProprio()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.aCarregar.set(false)),
      )
      .subscribe({
        next: (p) => this.perfil.set(p),
        error: (erro: ErroApi) => {
          this.perfil.set(null);
          this.erroCarregar.set(erro.message);
        },
      });
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

  editar(p: PerfilProprio) {
    this.ui.limpar();
    this.erro.set('');
    this.formulario.reset({
      nome: p.nome,
      curso: p.curso ?? '',
      instituicao: p.instituicao ?? '',
    });
    this.aEditar.set(true);
    focarDepoisDeDesenhar(this.injector, 'nome');
  }

  fecharEdicao() {
    this.ui.limpar();
    this.aEditar.set(false);
    focarDepoisDeDesenhar(this.injector, 'editar-perfil');
  }

  guardar() {
    if (this.aGuardar()) return;
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid) return;
    this.ui.limpar();
    this.erro.set('');
    this.aGuardar.set(true);
    const { nome, curso, instituicao } = this.formulario.getRawValue();
    this.api
      .atualizar({
        nome: nome.trim(),
        curso: curso.trim() || null,
        instituicao: instituicao.trim() || null,
      })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.aGuardar.set(false)),
      )
      .subscribe({
        next: (atualizado) => {
          // A resposta não traz as ligações nem as avaliações.
          this.perfil.update((p) => p && { ...p, ...atualizado });
          this.fecharEdicao();
          this.ui.sucesso('Perfil atualizado.');
        },
        error: (erro: ErroApi) => this.erro.set(erro.message),
      });
  }

  definirLigacoes(ligacoes: LigacaoPerfil[]) {
    this.perfil.update((p) => p && { ...p, ligacoesExternas: ligacoes });
  }
}
