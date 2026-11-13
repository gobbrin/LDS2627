/** Estados de uma viagem (P-04), com os nomes devolvidos pela API. */
export type EstadoViagem = 'Ativa' | 'Indisponibilizada' | 'Concluida';

/** Corpo de `POST /viagens` e de `PUT /viagens/{id}`. `partidaEm` em UTC. */
export interface DadosViagem {
  origem: string;
  destino: string;
  partidaEm: string;
  lugares: number;
}

export interface ViagemCriada {
  id: string;
  origem: string;
  destino: string;
  partidaEm: string;
  lugaresDisponiveis: number;
  distanciaKm: number | null;
  duracaoMinutos: number | null;
}

/** Viagem de `GET /viagens/minhas`, que só devolve as viagens ativas do condutor. */
export interface ViagemDoCondutor {
  id: string;
  origem: string;
  destino: string;
  partidaEm: string;
  lugaresOferecidos: number;
  lugaresDisponiveis: number;
}

/** Campos usados de `GET /viagens/{id}`, que não devolve os lugares oferecidos. */
export interface DetalhesViagem {
  id: string;
  origem: string;
  destino: string;
  partidaEm: string;
  lugaresDisponiveis: number;
  estado: EstadoViagem;
  distanciaKm: number | null;
  duracaoMinutos: number | null;
  condutorId: string;
}
