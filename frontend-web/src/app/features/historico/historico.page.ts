import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { RouterLink } from '@angular/router';
import { ApiClient } from '../../core/api-client';
import { PedidoEstado, DataPortugalPipe } from '../../shared/pedido-estado';
import { PesquisaApi } from '../pesquisa/pesquisa-api';
interface ViagemHistorica {
  id: string;
  origem: string;
  destino: string;
  partidaEm: string;
  estado: string;
}
interface Registo {
  id: string;
  origem?: string;
  destino?: string;
  estado?: string;
  conteudo?: string;
  titulo?: string;
}
interface Historico {
  viagensComoCondutor: ViagemHistorica[];
  viagensComoPassageiro: ViagemHistorica[];
  pedidosEfetuados: Registo[];
  pedidosRecebidos: Registo[];
  notificacoes: Registo[];
  avisos: Registo[];
}
interface Pessoa {
  id: string;
  nome: string;
}
@Component({
  imports: [DataPortugalPipe, FormsModule, RouterLink],
  template: `<h1>Histórico e avaliações</h1>
    <button (click)="carregar()" [disabled]="ocupado()">Atualizar histórico</button>
    @if (erro()) {
      <p role="alert">{{ erro() }}</p>
    }
    @if (aviso()) {
      <p role="status">{{ aviso() }}</p>
    }
    @if (historico(); as h) {
      @for (
        grupo of [
          { titulo: 'Viagens como condutor', viagens: h.viagensComoCondutor, condutor: true },
          { titulo: 'Viagens como passageiro', viagens: h.viagensComoPassageiro, condutor: false },
        ];
        track grupo.titulo
      ) {
        <h2>{{ grupo.titulo }}</h2>
        @for (v of grupo.viagens; track v.id) {
          <article class="card">
            <a [routerLink]="['/pesquisa', v.id]">{{ v.origem }} → {{ v.destino }}</a>
            <p>{{ v.partidaEm | dataPortugal }} · {{ v.estado }}</p>
            @if (v.estado === 'Concluida') {
              <button (click)="preparar(v, grupo.condutor)" [disabled]="ocupado()">
                Avaliar participante
              </button>
            }
          </article>
        } @empty {
          <p>Sem viagens.</p>
        }
      }
      @for (
        grupo of [
          { titulo: 'Pedidos efetuados', itens: h.pedidosEfetuados },
          { titulo: 'Pedidos recebidos', itens: h.pedidosRecebidos },
          { titulo: 'Notificações', itens: h.notificacoes },
          { titulo: 'Avisos', itens: h.avisos },
        ];
        track grupo.titulo
      ) {
        <h2>{{ grupo.titulo }}</h2>
        @for (r of grupo.itens; track r.id) {
          <p>{{ r.titulo }} {{ r.origem }} {{ r.destino }} {{ r.estado }} {{ r.conteudo }}</p>
        } @empty {
          <p>Sem registos.</p>
        }
      }
    }
    @if (viagem()) {
      <h2>Avaliação de uma viagem concluída</h2>
      <form (ngSubmit)="avaliar()">
        <label
          >Participante<select name="pessoa" [(ngModel)]="avaliadoId" required>
            <option value="">Selecionar</option>
            @for (p of pessoas(); track p.id) {
              <option [value]="p.id">{{ p.nome }}</option>
            }
          </select></label
        ><label
          >Classificação (1 a 5)<input
            name="classificacao"
            type="number"
            min="1"
            max="5"
            step="1"
            [(ngModel)]="classificacao"
            required /></label
        ><label
          ><input name="editar" type="checkbox" [(ngModel)]="editar" />Alterar a minha avaliação
          existente deste utilizador</label
        >
        <p>
          A identidade do avaliador não é mostrada no perfil público. A API atual não permite
          consultar aqui a tua avaliação anterior.
        </p>
        <button [disabled]="ocupado() || !avaliadoId || classificacao < 1 || classificacao > 5">
          {{ editar ? 'Atualizar avaliação' : 'Publicar avaliação' }}
        </button>
      </form>
    }`,
})
export class HistoricoPage extends PedidoEstado {
  private readonly api = inject(ApiClient);
  private readonly pesquisa = inject(PesquisaApi);
  readonly historico = signal<Historico | null>(null);
  readonly viagem = signal<string | null>(null);
  readonly pessoas = signal<Pessoa[]>([]);
  avaliadoId = '';
  classificacao = 5;
  editar = false;
  constructor() {
    super();
    this.carregar();
  }
  carregar() {
    this.executar(this.api.get<Historico>('historico', undefined, { erroNaPagina: true }), (h) =>
      this.historico.set(h),
    );
  }
  preparar(v: ViagemHistorica, condutor: boolean) {
    this.viagem.set(v.id);
    this.pessoas.set([]);
    this.avaliadoId = '';
    this.editar = false;
    if (condutor) {
      this.executar(
        this.api.get<{ passageiroId: string; nome: string }[]>(`viagens/${v.id}/passageiros`),
        (p) => this.pessoas.set(p.map((x) => ({ id: x.passageiroId, nome: x.nome }))),
      );
    } else
      this.executar(this.pesquisa.detalhe(v.id), (d) =>
        this.pessoas.set([{ id: d.condutorId, nome: d.condutor.nome }]),
      );
  }
  avaliar() {
    if (
      !this.viagem() ||
      !this.pessoas().some((p) => p.id === this.avaliadoId) ||
      !Number.isInteger(this.classificacao) ||
      this.classificacao < 1 ||
      this.classificacao > 5
    )
      return;
    const req = this.editar
      ? this.api.put(
          `avaliacoes/utilizadores/${this.avaliadoId}`,
          { classificacao: this.classificacao },
          { erroNaPagina: true },
        )
      : this.api.post(
          'avaliacoes',
          {
            viagemId: this.viagem(),
            avaliadoId: this.avaliadoId,
            classificacao: this.classificacao,
          },
          { erroNaPagina: true },
        );
    this.executar(req, () => this.aviso.set('Avaliação guardada.'));
  }
}
