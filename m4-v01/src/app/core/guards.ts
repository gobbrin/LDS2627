import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessaoService } from './sessao.service';

export function destinoInterno(valor: string | null): string {
  // Apenas destinos implementados: impede redirecionamentos externos e ciclos no login.
  return valor === '/perfil' || valor === '/inicio' ? valor : '/inicio';
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
