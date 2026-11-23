import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ActivatedRoute } from '@angular/router';
import { ApiClient } from '../../core/api-client';
import { SessaoService } from '../../core/sessao.service';
import { PedidoEstado, DataPortugalPipe } from '../../shared/pedido-estado';
interface Conversa {
  outroUtilizadorId: string;
  outroUtilizadorNome: string;
  ultimaMensagem: string;
  ultimaMensagemEm: string;
}
interface Mensagem {
  id: string;
  remetenteId: string;
  conteudo: string;
  enviadoEm: string;
}
@Component({
  imports: [DataPortugalPipe, FormsModule],
  template: `<h1>Mensagens individuais</h1>
    @if (erro()) {
      <p role="alert">{{ erro() }}</p>
    }
    <button (click)="listar()" [disabled]="ocupado()">Atualizar conversas</button>
    @for (c of conversas(); track c.outroUtilizadorId) {
      <button (click)="abrir(c.outroUtilizadorId, c.outroUtilizadorNome)" [disabled]="ocupado()">
        {{ c.outroUtilizadorNome }} · {{ c.ultimaMensagem }}
      </button>
    } @empty {
      <p>Sem conversas. Podes iniciar uma mensagem no perfil público de um utilizador.</p>
    }
    @if (destinatario()) {
      <h2>{{ nome() || 'Conversa individual' }}</h2>
      <button (click)="abrir(destinatario(), nome())" [disabled]="ocupado()">
        Atualizar mensagens
      </button>
      @for (m of mensagens(); track m.id) {
        <article class="card">
          <strong>{{ m.remetenteId === sessao.utilizador()?.id ? 'Tu' : nome() }}</strong>
          <p style="white-space:pre-wrap">{{ m.conteudo }}</p>
          <small>{{ m.enviadoEm | dataPortugal }}</small>
        </article>
      }
      <form (ngSubmit)="enviar()">
        <label
          >Mensagem<textarea
            name="conteudo"
            [(ngModel)]="conteudo"
            required
            maxlength="1000"
          ></textarea></label
        ><button [disabled]="ocupado() || !conteudo.trim()">Enviar mensagem</button>
      </form>
    }`,
})
export class MensagensPage extends PedidoEstado {
  private readonly api = inject(ApiClient);
  readonly sessao = inject(SessaoService);
  readonly conversas = signal<Conversa[]>([]);
  readonly mensagens = signal<Mensagem[]>([]);
  readonly destinatario = signal('');
  readonly nome = signal('');
  conteudo = '';
  constructor() {
    super();
    const id = inject(ActivatedRoute).snapshot.queryParamMap.get('destinatario');
    this.listar();
    if (id && /^[0-9a-f-]{36}$/i.test(id)) {
      this.destinatario.set(id);
    }
  }
  listar() {
    this.executar(
      this.api.get<Conversa[]>('mensagens/conversas', undefined, { erroNaPagina: true }),
      (cs) => {
        this.conversas.set(cs);
        if (this.destinatario())
          queueMicrotask(() =>
            this.abrir(
              this.destinatario(),
              cs.find((c) => c.outroUtilizadorId === this.destinatario())?.outroUtilizadorNome ??
                'Destinatário',
            ),
          );
      },
    );
  }
  abrir(id: string, nome: string) {
    if (this.ocupado()) return;
    this.destinatario.set(id);
    this.nome.set(nome);
    this.mensagens.set([]);
    this.executar(
      this.api.get<Mensagem[]>(`mensagens/conversa/${id}`, undefined, { erroNaPagina: true }),
      (ms) => this.mensagens.set(ms),
    );
  }
  enviar() {
    const texto = this.conteudo.trim();
    if (!texto || texto.length > 1000 || this.destinatario() === this.sessao.utilizador()?.id)
      return;
    this.executar(
      this.api.post(
        'mensagens',
        { destinatarioId: this.destinatario(), conteudo: texto },
        { erroNaPagina: true },
      ),
      () => {
        this.conteudo = '';
        queueMicrotask(() => this.abrir(this.destinatario(), this.nome()));
      },
    );
  }
}
