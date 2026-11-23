import { Injectable, inject, effect, signal, DestroyRef } from '@angular/core';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { CONFIGURACAO } from '../../core/configuracao';
import { SessaoService } from '../../core/sessao.service';
@Injectable({ providedIn: 'root' })
export class TempoRealService {
  private readonly sessao = inject(SessaoService);
  private readonly config = inject(CONFIGURACAO);
  readonly estado = signal('Sem ligação');
  readonly alteracoes = new Subject<void>();
  constructor() {
    inject(DestroyRef).onDestroy(() => this.alteracoes.complete());
    effect((onCleanup) => {
      if (!this.sessao.autenticado()) {
        this.estado.set('Sem sessão');
        return;
      }
      let cancelado = false;
      let retry: ReturnType<typeof setTimeout> | undefined;
      const ligacao = new HubConnectionBuilder()
        .withUrl(new URL('/hubs/pedidos', this.config.apiBaseUrl).href, {
          accessTokenFactory: () => this.sessao.obterToken() ?? '',
        })
        .withAutomaticReconnect([0, 2000, 10000, 30000])
        .configureLogging(LogLevel.None)
        .build();
      const atualizar = () => {
        if (!cancelado && this.sessao.autenticado()) this.alteracoes.next();
      };
      for (const nome of ['PedidoRecebido', 'PedidoAceite', 'PedidoRejeitado'])
        ligacao.on(nome, atualizar);
      ligacao.onreconnecting(() => {
        if (!cancelado) this.estado.set('A restabelecer ligação…');
      });
      ligacao.onreconnected(() => {
        if (!cancelado) {
          this.estado.set('Ligado');
          atualizar();
        }
      });
      const iniciar = async () => {
        if (cancelado) return;
        this.estado.set('A ligar…');
        try {
          await ligacao.start();
          if (cancelado) {
            await ligacao.stop();
            return;
          }
          this.estado.set('Ligado');
          atualizar();
        } catch {
          if (!cancelado) {
            this.estado.set('Sem ligação imediata. Podes atualizar manualmente.');
            retry = setTimeout(() => void iniciar(), 15000);
          }
        }
      };
      ligacao.onclose(() => {
        if (!cancelado) retry = setTimeout(() => void iniciar(), 15000);
      });
      void iniciar();
      onCleanup(() => {
        cancelado = true;
        clearTimeout(retry);
        void ligacao.stop();
      });
    });
  }
}
