export interface LigacaoPerfil {
  id: string;
  url: string;
  descricao: string | null;
}

export interface NovaLigacao {
  url: string;
  descricao: string | null;
}

export interface PerfilProprio {
  id: string;
  nome: string;
  email: string;
  emailConfirmado: boolean;
  curso: string | null;
  instituicao: string | null;
  criadoEm: string;
  ligacoesExternas: LigacaoPerfil[];
  /** `null` significa «sem avaliações». */
  classificacaoMedia: number | null;
  totalAvaliacoes: number;
}

/** Só os campos de P-10: nunca inclui o email. */
export interface PerfilPublico {
  id: string;
  nome: string;
  curso: string | null;
  instituicao: string | null;
  ligacoesExternas: LigacaoPerfil[];
  classificacaoMedia: number | null;
  totalAvaliacoes: number;
}

export interface PerfilAtualizado {
  id: string;
  nome: string;
  email: string;
  emailConfirmado: boolean;
  curso: string | null;
  instituicao: string | null;
}

export interface AlteracaoPerfil {
  nome: string;
  curso: string | null;
  instituicao: string | null;
}
