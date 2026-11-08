import { HttpErrorResponse } from '@angular/common/http';
export class ErroApi extends Error {
  constructor(
    readonly status: number,
    mensagem: string,
    readonly campos: Record<string, string[]> = {},
  ) {
    super(mensagem);
  }
}
export function normalizarErro(resposta: HttpErrorResponse): ErroApi {
  const alternativas: Record<number, string> = {
    0: 'Não foi possível contactar o servidor. Verifica a ligação e tenta novamente.',
    400: 'Confirma os dados introduzidos e tenta novamente.',
    401: 'A sessão não é válida. Inicia sessão novamente.',
    403: 'Não tens autorização para esta operação. Confirma se a conta está confirmada.',
    404: 'A informação pedida não está disponível.',
    409: 'Não foi possível concluir a operação. Atualiza a página e tenta novamente.',
    429: 'Foram feitos demasiados pedidos. Aguarda um momento e tenta novamente.',
    503: 'O serviço está temporariamente indisponível. Tenta novamente mais tarde.',
  };
  const corpo = resposta.error;
  let mensagem =
    alternativas[resposta.status] ?? 'Ocorreu um erro no servidor. Tenta novamente mais tarde.';
  const campos: Record<string, string[]> = {};
  // Só mensagens de erros previstos; respostas 5xx nunca expõem detalhes internos.
  if (resposta.status >= 400 && resposta.status < 500 && corpo && typeof corpo === 'object') {
    const erro = corpo.erro;
    const texto = typeof erro === 'string' ? erro : (erro?.mensagem ?? corpo.detail);
    if (typeof texto === 'string' && texto.trim()) mensagem = texto.slice(0, 500);
    const origem = erro?.campos ?? corpo.errors;
    if (origem && typeof origem === 'object') {
      for (const [campo, valor] of Object.entries(origem)) {
        const lista =
          typeof valor === 'string'
            ? [valor]
            : Array.isArray(valor)
              ? valor.filter((v) => typeof v === 'string')
              : [];
        if (lista.length) campos[campo] = lista.map((v) => v.slice(0, 500));
      }
    }
  }
  return new ErroApi(resposta.status, mensagem, campos);
}
