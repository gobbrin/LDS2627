import { Injectable, inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { ApiClient } from '../../core/api-client';

/** Número de viagens por página na pesquisa (a API aceita até 100). */
export const TAMANHO_PAGINA = 20;

/**
 * Filtros da pesquisa (RF-F003-01). Todos opcionais; `data` (AAAA-MM-DD) e `hora` (HH:mm) são
 * na hora de Portugal, que é como a API os interpreta.
 */
export interface FiltroPesquisa {
  origem: string;
  destino: string;
  data: string;
  hora: string;
}

/** Condutor como a API o mostra a outros utilizadores: só dados públicos. */
export interface CondutorPublico {
  id: string;
  nome: string;
  classificacaoMedia: number | null;
}

/** Média do condutor com uma casa decimal, ou o aviso de que ainda não foi avaliado. */
export function textoClassificacao(condutor: CondutorPublico | null): string {
  const media = condutor?.classificacaoMedia;
  if (media === null || media === undefined) return 'Sem avaliações';
  const texto = media.toLocaleString('pt-PT', {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  });
  return `Classificação ${texto}`;
}

/** Viagem de `GET /pesquisa/viagens` (`ViagemEncontrada` no backend). */
export interface ViagemEncontrada {
  id: string;
  origem: string;
  destino: string;
  partidaEm: string;
  lugaresDisponiveis: number;
  distanciaKm: number | null;
  duracaoMinutos: number | null;
  pontosAdicionais: number;
  condutor: CondutorPublico | null;
}

/** Resposta paginada de `GET /pesquisa/viagens`. */
export interface ResultadoPesquisa {
  total: number;
  pagina: number;
  tamanho: number;
  viagens: ViagemEncontrada[];
}

/** Estimativa de custo (RF-F008-10, RF-F008-11). É sempre indicativa: não é um preço. */
export interface EstimativaCusto {
  custoTotalEstimado: number;
  custoPorParticipante: number;
  totalParticipantes: number;
  moeda: string;
  indicativo: boolean;
}

/** Detalhe de `GET /viagens/{id}` (`DetalhesViagem` no backend). */
export interface DetalheViagem extends ViagemEncontrada {
  estado: string;
  condutorId: string;
  estimativaCusto: EstimativaCusto | null;
  estimativaIndisponivelMotivo: string | null;
}

/** Ponto da rota (`PontoDaRota`). `tipo`: Origem, Recolha, DestinoPassageiro ou DestinoViagem. */
export interface PontoDaRota {
  ordem: number;
  tipo: string;
  local: string;
  latitude: number;
  longitude: number;
}

/**
 * Rota de `GET /rotas/viagens/{id}` (`RotaDaViagem`). A API só devolve os pontos que o
 * utilizador pode ver (RF-F008-09, P-13). `geometria` é a linha do mapa, em pares [latitude, longitude];
 * `geometriaAproximada` diz que a linha são só retas entre os pontos.
 */
export interface RotaDaViagem {
  pontosOrdenados: PontoDaRota[];
  distanciaTotalKm: number;
  duracaoTotalMinutos: number;
  geometria: number[][];
  geometriaAproximada: boolean;
}

/** Pedidos à API usados pela pesquisa e pelo detalhe da viagem (F003 e F008). */
@Injectable({ providedIn: 'root' })
export class PesquisaApi {
  private readonly api = inject(ApiClient);

  /** Pesquisa viagens; só envia os filtros preenchidos. */
  pesquisar(filtro: FiltroPesquisa, pagina: number) {
    let params = new HttpParams().set('pagina', pagina).set('tamanho', TAMANHO_PAGINA);
    if (filtro.origem.trim()) params = params.set('origem', filtro.origem.trim());
    if (filtro.destino.trim()) params = params.set('destino', filtro.destino.trim());
    if (filtro.data) params = params.set('data', filtro.data);
    if (filtro.hora) params = params.set('hora', filtro.hora);
    return this.api.get<ResultadoPesquisa>('pesquisa/viagens', params, { erroNaPagina: true });
  }

  /** Detalhe público da viagem, com a estimativa de custo. */
  detalhe(id: string) {
    return this.api.get<DetalheViagem>(`viagens/${id}`, undefined, { erroNaPagina: true });
  }

  /** Rota da viagem, com os pontos que o utilizador pode ver e a linha do mapa. */
  rota(id: string) {
    return this.api.get<RotaDaViagem>(`rotas/viagens/${id}`, undefined, { erroNaPagina: true });
  }
}
