import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { CONFIGURACAO, pertenceApi } from './configuracao';
import { SessaoService } from './sessao.service';
import { NotificacaoUiService } from './notificacao-ui.service';
import { normalizarErro } from './erro-api';

export const autenticacaoInterceptor: HttpInterceptorFn = (pedido, seguinte) => {
  const config = inject(CONFIGURACAO);
  const sessao = inject(SessaoService);
  const publico = /\/autenticacao\/(login|registo|confirmar|reenviar-confirmacao)$/.test(
    pedido.url,
  );
  if (!pertenceApi(pedido.url, config) || publico) return seguinte(pedido);
  const token = sessao.obterToken();
  return seguinte(
    token ? pedido.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : pedido,
  );
};

export const erroInterceptor: HttpInterceptorFn = (pedido, seguinte) => {
  const config = inject(CONFIGURACAO);
  const sessao = inject(SessaoService);
  const ui = inject(NotificacaoUiService);
  const router = inject(Router);
  return seguinte(pedido).pipe(
    catchError((resposta: unknown) => {
      if (!pertenceApi(pedido.url, config) || !(resposta instanceof HttpErrorResponse))
        return throwError(() => resposta);
      const erro = normalizarErro(resposta);
      // Uma resposta atrasada de uma sessão antiga não termina uma sessão mais recente.
      if (
        resposta.status === 401 &&
        pedido.headers.get('Authorization') === `Bearer ${sessao.obterToken()}`
      ) {
        sessao.terminar();
        void router.navigate(['/entrar']);
      }
      ui.erro(erro.message);
      return throwError(() => erro);
    }),
  );
};
