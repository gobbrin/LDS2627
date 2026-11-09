export interface PerfilProprio {
  id: string;
  nome: string;
  email: string;
  emailConfirmado: boolean;
  curso: string | null;
  instituicao: string | null;
  criadoEm: string;
  ligacoesExternas: { id: string; url: string; descricao: string | null }[];
  classificacaoMedia: number | null;
  totalAvaliacoes: number;
}
