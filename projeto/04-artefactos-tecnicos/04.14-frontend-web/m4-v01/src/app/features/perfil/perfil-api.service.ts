import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../core/api-client';
import {
  AlteracaoPerfil,
  LigacaoPerfil,
  NovaLigacao,
  PerfilAtualizado,
  PerfilProprio,
  PerfilPublico,
} from '../../shared/models/perfil';

@Injectable({ providedIn: 'root' })
export class PerfilApiService {
  private readonly api = inject(ApiClient);
  obterProprio() {
    return this.api.get<PerfilProprio>('utilizadores/me', undefined, { erroNaPagina: true });
  }
  atualizar(dados: AlteracaoPerfil) {
    return this.api.put<PerfilAtualizado>('utilizadores/me', dados, { erroNaPagina: true });
  }
  adicionarLigacao(ligacao: NovaLigacao) {
    return this.api.post<LigacaoPerfil>('utilizadores/me/ligacoes', ligacao, {
      erroNaPagina: true,
    });
  }
  removerLigacao(id: string) {
    return this.api.delete<void>(`utilizadores/me/ligacoes/${encodeURIComponent(id)}`, {
      erroNaPagina: true,
    });
  }
  obterPublico(id: string) {
    return this.api.get<PerfilPublico>(`utilizadores/${encodeURIComponent(id)}`, undefined, {
      erroNaPagina: true,
    });
  }
}
