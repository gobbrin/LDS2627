import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { CONFIGURACAO, pertenceApi } from './configuracao';
import { SessaoService } from './sessao.service';
import { NotificacaoUiService } from './notificacao-ui.service';
import { ERRO_NA_PAGINA, normalizarErro } from './erro-api';

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
      const autorizacao = pedido.headers.get('Authorization');
      if (resposta.status === 401 && autorizacao) {
        // Uma resposta atrasada de uma sessão antiga não afeta uma sessão mais recente.
        if (autorizacao === `Bearer ${sessao.obterToken()}`) {
          sessao.terminar();
          ui.erro(erro.message, { manterAoNavegar: true });
          void router.navigate(['/entrar']);
        }
      } else if (!pedido.context.get(ERRO_NA_PAGINA)) {
        ui.erro(erro.message);
      }
      return throwError(() => erro);
    }),
  );
};
