import { InjectionToken } from '@angular/core';

export interface Configuracao {
  apiBaseUrl: string;
}
export const CONFIGURACAO = new InjectionToken<Configuracao>('CONFIGURACAO');

export function validarConfiguracao(valor: unknown): Configuracao {
  if (
    !valor ||
    typeof valor !== 'object' ||
    !('apiBaseUrl' in valor) ||
    typeof valor.apiBaseUrl !== 'string'
  ) {
    throw new Error('A configuração não contém o endereço da API.');
  }
  const url = new URL(valor.apiBaseUrl);
  const local = ['localhost', '127.0.0.1', '[::1]'].includes(url.hostname);
  if (
    (url.protocol !== 'https:' && !(local && url.protocol === 'http:')) ||
    url.username ||
    url.password ||
    url.search ||
    url.hash ||
    !/^\/api\/v1\/?$/.test(url.pathname)
  ) {
    throw new Error('Usa um endereço HTTPS terminado em /api/v1 (HTTP apenas em localhost).');
  }
  return { apiBaseUrl: url.href.replace(/\/$/, '') };
}

export function pertenceApi(url: string, config: Configuracao): boolean {
  return url.startsWith(config.apiBaseUrl + '/') || url === config.apiBaseUrl;
}

export async function carregarConfiguracao(): Promise<Configuracao> {
  const resposta = await fetch(new URL('config.json', document.baseURI), { cache: 'no-store' });
  if (!resposta.ok) throw new Error('Não foi possível carregar a configuração.');
  return validarConfiguracao(await resposta.json());
}
