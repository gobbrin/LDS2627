export interface DadosRegisto {
  nome: string;
  email: string;
  password: string;
}
export interface Credenciais {
  email: string;
  password: string;
}
/** Resposta de registo e reenvio: um texto para mostrar ao utilizador. */
export interface RespostaMensagem {
  mensagem: string;
}
export interface TokenAcesso {
  accessToken: string;
  expiresIn: number;
}
export interface Sessao {
  accessToken: string;
  expiraEm: number;
  utilizador: { id: string };
}
export interface SessaoAtual {
  id: string;
}
