import { Injectable, signal } from '@angular/core';
@Injectable({ providedIn: 'root' })
export class NotificacaoUiService {
  readonly mensagem = signal<{ tipo: 'erro' | 'sucesso'; texto: string } | null>(null);
  sucesso(texto: string) {
    this.mensagem.set({ tipo: 'sucesso', texto });
  }
  erro(texto: string) {
    this.mensagem.set({ tipo: 'erro', texto });
  }
  limpar() {
    this.mensagem.set(null);
  }
}
