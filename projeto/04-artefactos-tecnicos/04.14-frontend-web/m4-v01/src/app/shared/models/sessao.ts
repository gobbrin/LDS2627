export interface Credenciais {
  email: string;
  password: string;
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
