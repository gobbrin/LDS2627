import {
  Component,
  computed,
  DestroyRef,
  inject,
  Injector,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ErroApi } from '../../core/erro-api';
import { NotificacaoUiService } from '../../core/notificacao-ui.service';
import { ErroFormularioComponent } from '../../shared/erro-formulario.component';
import { focarDepoisDeDesenhar } from '../../shared/focar';
import { LigacaoPerfil } from '../../shared/models/perfil';
import { urlHttp } from '../../shared/validadores';
import { PerfilApiService } from './perfil-api.service';

/** P-27: número máximo de ligações externas por utilizador. */
const MAXIMO_LIGACOES = 5;

type Campo = 'url' | 'descricao';

const FORMATO_URL = 'Indica um endereço completo, a começar por http:// ou https://.';

const MENSAGENS: Record<Campo, Record<string, string>> = {
  url: {
    required: 'Indica o endereço da ligação.',
    urlHttp: FORMATO_URL,
    maxlength: 'O endereço tem no máximo 2048 caracteres.',
  },
  descricao: { maxlength: 'A descrição tem no máximo 100 caracteres.' },
};

@Component({
  selector: 'app-ligacoes-perfil',
  imports: [ReactiveFormsModule, ErroFormularioComponent],
  template: `<section class="gestao-ligacoes" aria-labelledby="titulo-ligacoes">
    <h2 id="titulo-ligacoes" tabindex="-1">Ligações externas</h2>
    <p class="muted">
      Tens {{ ligacoes().length }} de {{ maximo }}. As ligações são guardadas ou removidas logo que
      o confirmas.
    </p>
    @if (ligacoes().length) {
      <ul class="ligacoes">
        @for (ligacao of ligacoes(); track ligacao.id) {
          <li>
            <div>
              <a [href]="ligacao.url" target="_blank" rel="noopener noreferrer">{{
                ligacao.descricao || ligacao.url
              }}</a>
              @if (ligacao.descricao) {
                <small class="muted">{{ ligacao.url }}</small>
              }
            </div>
            <div class="acoes">
              @if (aConfirmar() === ligacao.id) {
                <span>Remover esta ligação?</span>
                <button
                  class="perigo"
                  type="button"
                  [disabled]="aRemover()"
                  (click)="remover(ligacao)"
                >
                  {{ aRemover() === ligacao.id ? 'A remover…' : 'Sim, remover' }}
                </button>
                <button
                  [id]="'manter-' + ligacao.id"
                  type="button"
                  [disabled]="aRemover()"
                  (click)="naoRemover(ligacao)"
                >
                  Não
                </button>
              } @else {
                <button
                  [id]="'remover-' + ligacao.id"
                  type="button"
                  [disabled]="aRemover()"
                  [attr.aria-label]="'Remover ' + (ligacao.descricao || ligacao.url)"
                  (click)="pedirConfirmacao(ligacao)"
                >
                  Remover
                </button>
              }
            </div>
          </li>
        }
      </ul>
    } @else {
      <p class="muted">Ainda não tens ligações externas.</p>
    }
    @if (erroRemocao()) {
      <p class="field-error" role="alert">{{ erroRemocao() }}</p>
    }
    @if (atingiuMaximo()) {
      <p class="notice" role="status">
        Atingiste o máximo de {{ maximo }} ligações. Remove uma para acrescentares outra.
      </p>
    } @else {
      <form
        [formGroup]="formulario"
        (ngSubmit)="adicionar()"
        [attr.aria-busy]="aAdicionar()"
        novalidate
      >
        <label for="url">Endereço</label>
        <input
          id="url"
          type="url"
          formControlName="url"
          placeholder="https://www.linkedin.com/in/…"
          autocomplete="url"
          [attr.aria-invalid]="invalido('url')"
          aria-describedby="erro-url"
        />
        <div id="erro-url"><app-erro-formulario [erros]="erros('url')" /></div>
        <label for="descricao">Descrição <span class="muted">(opcional)</span></label>
        <input
          id="descricao"
          formControlName="descricao"
          autocomplete="off"
          [attr.aria-invalid]="invalido('descricao')"
          aria-describedby="erro-descricao"
        />
        <div id="erro-descricao"><app-erro-formulario [erros]="erros('descricao')" /></div>
        @if (erroAdicao()) {
          <p class="field-error" role="alert">{{ erroAdicao() }}</p>
        }
        <div class="acoes">
          <button type="submit" [disabled]="aAdicionar()">
            {{ aAdicionar() ? 'A adicionar…' : 'Adicionar ligação' }}
          </button>
        </div>
      </form>
    }
  </section>`,
})
export class LigacoesPerfilComponent {
  private readonly api = inject(PerfilApiService);
  private readonly ui = inject(NotificacaoUiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  readonly ligacoes = input.required<LigacaoPerfil[]>();
  readonly alteradas = output<LigacaoPerfil[]>();
  readonly maximo = MAXIMO_LIGACOES;
  readonly atingiuMaximo = computed(() => this.ligacoes().length >= MAXIMO_LIGACOES);
  readonly formulario = inject(FormBuilder).nonNullable.group({
    url: ['', [Validators.required, urlHttp, Validators.maxLength(2048)]],
    descricao: ['', Validators.maxLength(100)],
  });
  readonly aAdicionar = signal(false);
  readonly erroAdicao = signal('');
  /** Ligação à espera de confirmação da remoção (UC-F001-09, passo 2). */
  readonly aConfirmar = signal<string | null>(null);
  readonly aRemover = signal<string | null>(null);
  readonly erroRemocao = signal('');

  invalido(campo: Campo) {
    const controlo = this.formulario.controls[campo];
    return controlo.touched && controlo.invalid;
  }

  erros(campo: Campo): string[] {
    if (!this.invalido(campo)) return [];
    const [primeiro] = Object.keys(this.formulario.controls[campo].errors ?? {});
    return [MENSAGENS[campo][primeiro]];
  }

  adicionar() {
    if (this.aAdicionar()) return;
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid) return;
    this.ui.limpar();
    this.erroAdicao.set('');
    this.erroRemocao.set('');
    this.aAdicionar.set(true);
    const { url, descricao } = this.formulario.getRawValue();
    this.api
      .adicionarLigacao({ url: url.trim(), descricao: descricao.trim() || null })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.aAdicionar.set(false)),
      )
      .subscribe({
        next: (nova) => {
          this.formulario.reset();
          const lista = [...this.ligacoes(), nova];
          this.alteradas.emit(lista);
          this.ui.sucesso('Ligação adicionada.');
          // No limite o formulário desaparece, e com ele o botão que tinha o foco.
          if (lista.length >= MAXIMO_LIGACOES) this.focar('titulo-ligacoes');
        },
        error: (erro: ErroApi) => {
          this.erroAdicao.set(erro.message);
          // UC-F001-08 E2: o limite pode ter sido atingido noutro dispositivo.
          if (erro.status === 400) this.sincronizar();
        },
      });
  }

  pedirConfirmacao(ligacao: LigacaoPerfil) {
    this.ui.limpar();
    this.erroRemocao.set('');
    this.aConfirmar.set(ligacao.id);
    this.focar(`manter-${ligacao.id}`);
  }

  naoRemover(ligacao: LigacaoPerfil) {
    this.erroRemocao.set('');
    this.aConfirmar.set(null);
    this.focar(`remover-${ligacao.id}`);
  }

  remover(ligacao: LigacaoPerfil) {
    if (this.aRemover()) return;
    this.erroRemocao.set('');
    this.aRemover.set(ligacao.id);
    this.api
      .removerLigacao(ligacao.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.aRemover.set(null)),
      )
      .subscribe({
        next: () => {
          this.retirar(ligacao.id);
          this.ui.sucesso('Ligação removida.');
        },
        error: (erro: ErroApi) => {
          this.erroRemocao.set(erro.message);
          // UC-F001-09 E1: já tinha sido removida, por exemplo noutro dispositivo.
          if (erro.status === 404) {
            this.retirar(ligacao.id);
            this.sincronizar();
          }
        },
      });
  }

  private retirar(id: string) {
    this.aConfirmar.set(null);
    this.erroAdicao.set('');
    this.alteradas.emit(this.ligacoes().filter((ligacao) => ligacao.id !== id));
    this.focar('titulo-ligacoes');
  }

  /** Atualiza só as ligações: os dados por guardar no formulário do perfil ficam intactos. */
  private sincronizar() {
    this.api
      .obterProprio()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (perfil) => this.alteradas.emit(perfil.ligacoesExternas),
        error: () => {
          /* A lista fica como está e o erro da operação já está à vista. */
        },
      });
  }

  private focar(id: string) {
    focarDepoisDeDesenhar(this.injector, id);
  }
}
