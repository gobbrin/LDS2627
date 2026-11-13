import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessaoService } from './sessao.service';

export function destinoInterno(valor: string | null): string {
  // Só caminhos internos: impede redirecionamentos externos e ciclos no login.
  const interno = valor !== null && /^\/(?![/\\])/.test(valor);
  return interno && !/^\/entrar([/?#]|$)/.test(valor) ? valor : '/inicio';
}
export const autenticadoGuard: CanActivateFn = (_route, state) => {
  return inject(SessaoService).obterToken()
    ? true
    : inject(Router).createUrlTree(['/entrar'], {
        queryParams: { regresso: destinoInterno(state.url) },
      });
};
export const visitanteGuard: CanActivateFn = () => {
  return inject(SessaoService).obterToken() ? inject(Router).createUrlTree(['/inicio']) : true;
};
