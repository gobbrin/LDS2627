import { HttpContextToken, HttpErrorResponse } from '@angular/common/http';

/** Marca um pedido cujo erro é apresentado pela própria página, junto do formulário. */
export const ERRO_NA_PAGINA = new HttpContextToken<boolean>(() => false);

export class ErroApi extends Error {
  constructor(
    readonly status: number,
    mensagem: string,
    readonly campos: Record<string, string[]> = {},
    /** Verdadeiro quando o texto veio da API; falso quando é a alternativa do cliente. */
    readonly mensagemDaApi = false,
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
  let mensagemDaApi = false;
  const campos: Record<string, string[]> = {};
  // A API tem mensagens próprias nos 4xx e nas falhas de serviços externos (502, 503, 504).
  // Os restantes 5xx ficam com o texto genérico, para nunca exporem detalhes internos.
  const previsto =
    (resposta.status >= 400 && resposta.status < 500) || [502, 503, 504].includes(resposta.status);
  if (previsto && corpo && typeof corpo === 'object') {
    const erro = corpo.erro;
    const texto = typeof erro === 'string' ? erro : (erro?.mensagem ?? corpo.detail);
    if (typeof texto === 'string' && texto.trim()) {
      mensagem = texto.slice(0, 500);
      mensagemDaApi = true;
    }
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
  return new ErroApi(resposta.status, mensagem, campos, mensagemDaApi);
}
