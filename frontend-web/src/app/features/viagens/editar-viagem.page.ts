import { Component, computed, DestroyRef, inject, Injector, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, forkJoin, Subscription } from 'rxjs';
import { ErroApi } from '../../core/erro-api';
import { NotificacaoUiService } from '../../core/notificacao-ui.service';
import { SessaoService } from '../../core/sessao.service';
import { EstadoBadgeComponent } from '../../shared/estado-badge.component';
import { focarDepoisDeDesenhar } from '../../shared/focar';
import { DadosViagem, DetalhesViagem } from '../../shared/models/viagem';
import { formatarPartida, formatarRota, paraCamposPortugal } from './partida';
import { ViagemApiService } from './viagem-api.service';
import { ValoresViagem, ViagemFormComponent } from './viagem-form.component';

const GUID = /^[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i;

@Component({
  selector: 'app-editar-viagem-page',
  imports: [RouterLink, EstadoBadgeComponent, ViagemFormComponent],
  template: `<section class="panel">
    <p class="eyebrow">AS TUAS VIAGENS</p>
    <h1 id="titulo-viagem" tabindex="-1">Gerir viagem</h1>
    @if (aCarregar()) {
      <p role="status">A carregar a viagem…</p>
    } @else if (viagem(); as v) {
      <dl>
        <div>
          <dt>Estado</dt>
          <dd><app-estado-badge [estado]="v.estado" /></dd>
        </div>
        <div>
          <dt>Percurso</dt>
          <dd>{{ v.origem }} <span aria-hidden="true">→</span> {{ v.destino }}</dd>
        </div>
        <div>
          <dt>Partida</dt>
          <dd>{{ partida(v) }}</dd>
        </div>
        <div>
          <dt>Lugares disponíveis</dt>
          <dd>{{ lugares(v) }}</dd>
        </div>
        @if (rota(v); as textoRota) {
          <div>
            <dt>Rota</dt>
            <dd>{{ textoRota }}</dd>
          </div>
        }
      </dl>
      @if (valores(); as inicial) {
        <h2>Alterar dados</h2>
        <app-viagem-form
          textoSubmeter="Guardar alterações"
          textoEmCurso="A guardar…"
          [inicial]="inicial"
          [ocupados]="ocupados()"
          [aGuardar]="aGuardar()"
          [erro]="erro()"
          (submetido)="guardar($event)"
        />
      } @else {
        <p class="muted">Esta viagem já não pode ser alterada.</p>
        @if (erro()) {
          <p class="field-error" role="alert">{{ erro() }}</p>
        }
      }
      @if (v.estado === 'Ativa') {
        <section class="zona-indisponibilizar" aria-labelledby="titulo-indisponibilizar">
          <h2 id="titulo-indisponibilizar">Indisponibilizar viagem</h2>
          @if (aConfirmar()) {
            <p>
              Os pedidos pendentes e as participações aceites serão cancelados. A viagem não é
              eliminada, mas deixa de poder ser alterada. Queres continuar?
            </p>
            <div class="acoes">
              <button
                class="perigo"
                type="button"
                [disabled]="aIndisponibilizar()"
                (click)="indisponibilizar()"
              >
                {{ aIndisponibilizar() ? 'A indisponibilizar…' : 'Sim, indisponibilizar' }}
              </button>
              <button
                id="manter-viagem"
                type="button"
                [disabled]="aIndisponibilizar()"
                (click)="naoIndisponibilizar()"
              >
                Não
              </button>
            </div>
          } @else {
            <p class="muted">A viagem deixa de estar disponível, mas o registo é preservado.</p>
            <button
              id="indisponibilizar-viagem"
              class="perigo"
              type="button"
              (click)="pedirConfirmacao()"
            >
              Indisponibilizar
            </button>
          }
          @if (erroIndisponibilizar()) {
            <p class="field-error" role="alert">{{ erroIndisponibilizar() }}</p>
          }
        </section>
      }
      <a routerLink="/viagens">Voltar às minhas viagens</a>
    } @else {
      <p role="alert">{{ erroCarregar() }}</p>
      @if (podeRepetir()) {
        <button type="button" (click)="carregar()">Tentar novamente</button>
      } @else {
        <a class="button" routerLink="/viagens">Voltar às minhas viagens</a>
      }
    }
  </section>`,
})
export class EditarViagemPage {
  private readonly api = inject(ViagemApiService);
  private readonly ui = inject(NotificacaoUiService);
  private readonly sessao = inject(SessaoService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  private id = '';
  private pedido?: Subscription;
  readonly viagem = signal<DetalhesViagem | null>(null);
  /** `GET /viagens/{id}` não traz os lugares oferecidos; vêm da lista das viagens ativas. */
  readonly oferecidos = signal<number | null>(null);
  readonly aCarregar = signal(true);
  readonly erroCarregar = signal('');
  readonly podeRepetir = signal(false);
  readonly aGuardar = signal(false);
  readonly erro = signal('');
  readonly aConfirmar = signal(false);
  readonly aIndisponibilizar = signal(false);
  readonly erroIndisponibilizar = signal('');

  readonly ocupados = computed(() => {
    const oferecidos = this.oferecidos();
    const viagem = this.viagem();
    return oferecidos === null || !viagem ? 0 : oferecidos - viagem.lugaresDisponiveis;
  });

  /** Só uma viagem ativa e ainda por partir é alterável (RF-F002-07, RF-F002-11). */
  readonly valores = computed<ValoresViagem | null>(() => {
    const viagem = this.viagem();
    const lugares = this.oferecidos();
    if (!viagem || lugares === null || viagem.estado !== 'Ativa') return null;
    if (new Date(viagem.partidaEm).getTime() <= Date.now()) return null;
    return {
      origem: viagem.origem,
      destino: viagem.destino,
      ...paraCamposPortugal(viagem.partidaEm),
      lugares,
    };
  });

  constructor() {
    // O Angular reutiliza a página quando só muda o :id.
    inject(ActivatedRoute)
      .paramMap.pipe(takeUntilDestroyed())
      .subscribe((parametros) => {
        this.id = parametros.get('id') ?? '';
        this.carregar();
      });
  }

  carregar(focarTitulo = false) {
    this.pedido?.unsubscribe();
    this.viagem.set(null);
    this.oferecidos.set(null);
    this.aConfirmar.set(false);
    this.aCarregar.set(true);
    // Um identificador mal formado nunca existe, e o ApiClient recusaria o caminho codificado.
    if (!GUID.test(this.id)) {
      this.falhar('Viagem não encontrada.', false);
      return;
    }
    this.pedido = forkJoin({
      viagem: this.api.consultar(this.id),
      minhas: this.api.listarMinhas(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ viagem, minhas }) => {
          this.aCarregar.set(false);
          // O backend volta a confirmar em cada operação; isto só evita mostrar ações inúteis.
          if (viagem.condutorId.toLowerCase() !== this.sessao.utilizador()?.id.toLowerCase()) {
            this.falhar('Só o condutor pode gerir esta viagem.', false);
            return;
          }
          this.viagem.set(viagem);
          this.oferecidos.set(minhas.find((v) => v.id === viagem.id)?.lugaresOferecidos ?? null);
          if (focarTitulo) focarDepoisDeDesenhar(this.injector, 'titulo-viagem');
        },
        error: (erro: ErroApi) =>
          this.falhar(
            erro.status === 404 ? 'Viagem não encontrada.' : erro.message,
            erro.status !== 404,
          ),
      });
  }

  partida(viagem: DetalhesViagem) {
    return formatarPartida(viagem.partidaEm);
  }

  rota(viagem: DetalhesViagem) {
    return formatarRota(viagem.distanciaKm, viagem.duracaoMinutos);
  }

  lugares(viagem: DetalhesViagem) {
    const oferecidos = this.oferecidos();
    return oferecidos === null
      ? String(viagem.lugaresDisponiveis)
      : `${viagem.lugaresDisponiveis} de ${oferecidos}`;
  }

  guardar(dados: DadosViagem) {
    if (this.aGuardar()) return;
    this.ui.limpar();
    this.erro.set('');
    this.erroIndisponibilizar.set('');
    this.aGuardar.set(true);
    this.api
      .alterar(this.id, dados)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.aGuardar.set(false)),
      )
      .subscribe({
        next: () => {
          this.ui.sucesso('Viagem atualizada.');
          this.carregar(true);
        },
        error: (erro: ErroApi) => {
          this.erro.set(erro.message);
          // A viagem mudou entretanto (noutro dispositivo ou por um passageiro aceite).
          if (erro.status === 409) this.carregar(true);
        },
      });
  }

  pedirConfirmacao() {
    this.ui.limpar();
    this.erroIndisponibilizar.set('');
    this.aConfirmar.set(true);
    focarDepoisDeDesenhar(this.injector, 'manter-viagem');
  }

  naoIndisponibilizar() {
    this.erroIndisponibilizar.set('');
    this.aConfirmar.set(false);
    focarDepoisDeDesenhar(this.injector, 'indisponibilizar-viagem');
  }

  indisponibilizar() {
    if (this.aIndisponibilizar()) return;
    this.erro.set('');
    this.erroIndisponibilizar.set('');
    this.aIndisponibilizar.set(true);
    this.api
      .indisponibilizar(this.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.aIndisponibilizar.set(false)),
      )
      .subscribe({
        next: () => {
          this.ui.sucesso('Viagem indisponibilizada.');
          this.carregar(true);
        },
        error: (erro: ErroApi) => {
          // A viagem já não está ativa: a mensagem fica junto do estado atualizado.
          if (erro.status === 409) {
            this.erro.set(erro.message);
            this.carregar(true);
          } else this.erroIndisponibilizar.set(erro.message);
        },
      });
  }

  private falhar(mensagem: string, podeRepetir: boolean) {
    this.erroCarregar.set(mensagem);
    this.podeRepetir.set(podeRepetir);
    this.aCarregar.set(false);
  }
}
