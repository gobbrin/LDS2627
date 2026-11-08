import { inject, Injectable } from '@angular/core';
import { catchError, finalize, of, switchMap, tap, throwError } from 'rxjs';
import { ApiClient } from '../../core/api-client';
import { SessaoService } from '../../core/sessao.service';
import { NotificacaoUiService } from '../../core/notificacao-ui.service';
import { ErroApi } from '../../core/erro-api';
import { Credenciais, SessaoAtual, TokenAcesso } from '../../shared/models/sessao';

@Injectable({ providedIn: 'root' })
export class AutenticacaoApiService {
  private readonly api = inject(ApiClient);
  private readonly sessao = inject(SessaoService);
  private readonly ui = inject(NotificacaoUiService);
  login(dados: Credenciais) {
    return this.api.post<TokenAcesso>('autenticacao/login', dados).pipe(
      tap((token) => this.sessao.iniciar(token)),
      switchMap(() => this.validar()),
      catchError((erro: unknown) => {
        this.sessao.terminar();
        const falha =
          erro instanceof ErroApi
            ? erro
            : new ErroApi(
                0,
                'Não foi possível guardar a sessão. Verifica se o armazenamento do browser está disponível e tenta novamente.',
              );
        this.ui.erro(falha.message);
        return throwError(() => falha);
      }),
    );
  }
  validar() {
    return this.api.get<SessaoAtual>('autenticacao/me').pipe(
      tap((atual) => {
        if (!atual.id || atual.id !== this.sessao.utilizador()?.id) {
          this.sessao.terminar();
          throw new ErroApi(401, 'Não foi possível validar a sessão. Inicia sessão novamente.');
        }
      }),
    );
  }
  logout() {
    return this.api.post<void>('autenticacao/logout').pipe(
      tap(() => this.ui.sucesso('Sessão terminada.')),
      catchError((erro: unknown) => {
        if (erro instanceof ErroApi && erro.status === 401)
          this.ui.sucesso('A sessão já não estava ativa.');
        else
          this.ui.erro(
            'A sessão foi terminada neste browser, mas o servidor não confirmou a revogação.',
          );
        return of(undefined);
      }),
      finalize(() => this.sessao.terminar()),
    );
  }
}
