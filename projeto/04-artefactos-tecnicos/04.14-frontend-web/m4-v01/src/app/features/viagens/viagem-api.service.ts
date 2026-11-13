import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../core/api-client';
import {
  DadosViagem,
  DetalhesViagem,
  ViagemCriada,
  ViagemDoCondutor,
} from '../../shared/models/viagem';

@Injectable({ providedIn: 'root' })
export class ViagemApiService {
  private readonly api = inject(ApiClient);
  criar(dados: DadosViagem) {
    return this.api.post<ViagemCriada>('viagens', dados, { erroNaPagina: true });
  }
  listarMinhas() {
    return this.api.get<ViagemDoCondutor[]>('viagens/minhas', undefined, { erroNaPagina: true });
  }
  consultar(id: string) {
    return this.api.get<DetalhesViagem>(`viagens/${encodeURIComponent(id)}`, undefined, {
      erroNaPagina: true,
    });
  }
  alterar(id: string, dados: DadosViagem) {
    return this.api.put<void>(`viagens/${encodeURIComponent(id)}`, dados, { erroNaPagina: true });
  }
  indisponibilizar(id: string) {
    return this.api.post<void>(`viagens/${encodeURIComponent(id)}/indisponibilizar`, null, {
      erroNaPagina: true,
    });
  }
}
