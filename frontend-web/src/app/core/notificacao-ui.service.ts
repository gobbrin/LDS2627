import { inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

export interface OpcoesNotificacao {
  /** A mensagem antecede um redirecionamento e tem de sobreviver a essa navegação. */
  manterAoNavegar?: boolean;
}

@Injectable({ providedIn: 'root' })
export class NotificacaoUiService {
  readonly mensagem = signal<{ tipo: 'erro' | 'sucesso'; texto: string } | null>(null);
  private manterNaProxima = false;

  constructor() {
    inject(Router)
      .events.pipe(
        filter((evento) => evento instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => {
        if (this.manterNaProxima) this.manterNaProxima = false;
        else this.limpar();
      });
  }

  sucesso(texto: string, opcoes?: OpcoesNotificacao) {
    this.mostrar('sucesso', texto, opcoes);
  }
  erro(texto: string, opcoes?: OpcoesNotificacao) {
    this.mostrar('erro', texto, opcoes);
  }
  limpar() {
    this.manterNaProxima = false;
    this.mensagem.set(null);
  }

  private mostrar(tipo: 'erro' | 'sucesso', texto: string, opcoes?: OpcoesNotificacao) {
    this.manterNaProxima = opcoes?.manterAoNavegar ?? false;
    this.mensagem.set({ tipo, texto });
  }
}
