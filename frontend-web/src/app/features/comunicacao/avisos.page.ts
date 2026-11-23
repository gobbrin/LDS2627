import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ActivatedRoute } from '@angular/router';
import { ApiClient } from '../../core/api-client';
import { SessaoService } from '../../core/sessao.service';
import { PedidoEstado, DataPortugalPipe } from '../../shared/pedido-estado';
import { PesquisaApi } from '../pesquisa/pesquisa-api';
interface Aviso {
  id: string;
  conteudo: string;
  criadoEm: string;
}
@Component({
  imports: [DataPortugalPipe, FormsModule],
  template: `<h1>Avisos da viagem</h1>
    @if (erro()) {
      <p role="alert">{{ erro() }}</p>
    }
    <button (click)="carregar()" [disabled]="ocupado()">Atualizar avisos</button>
    @if (condutor()) {
      <form (ngSubmit)="enviar()">
        <label
          >Aviso aos passageiros aceites<textarea
            name="conteudo"
            [(ngModel)]="conteudo"
            required
            maxlength="100"
          ></textarea></label
        ><button [disabled]="ocupado() || !conteudo.trim()">Publicar aviso</button>
      </form>
    }
    @for (a of avisos(); track a.id) {
      <article class="card">
        <p style="white-space:pre-wrap">{{ a.conteudo }}</p>
        <small>{{ a.criadoEm | dataPortugal }}</small>
      </article>
    } @empty {
      <p>Sem avisos disponíveis.</p>
    }`,
})
export class AvisosPage extends PedidoEstado {
  private readonly api = inject(ApiClient);
  private readonly pesquisa = inject(PesquisaApi);
  private readonly sessao = inject(SessaoService);
  readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id') ?? '';
  readonly condutor = signal(false);
  readonly avisos = signal<Aviso[]>([]);
  conteudo = '';
  constructor() {
    super();
    this.executar(this.pesquisa.detalhe(this.id), (v) => {
      this.condutor.set(v.condutorId === this.sessao.utilizador()?.id);
      queueMicrotask(() => this.carregar());
    });
  }
  carregar() {
    this.executar(
      this.api.get<Aviso[]>(`viagens/${this.id}/avisos`, undefined, { erroNaPagina: true }),
      (a) => this.avisos.set(a),
    );
  }
  enviar() {
    const conteudo = this.conteudo.trim();
    if (!conteudo || conteudo.length > 100 || !this.condutor()) return;
    this.executar(
      this.api.post(`viagens/${this.id}/avisos`, { conteudo }, { erroNaPagina: true }),
      () => {
        this.conteudo = '';
        queueMicrotask(() => this.carregar());
      },
    );
  }
}
