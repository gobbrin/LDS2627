import { inject, Injectable } from '@angular/core';
import { catchError, map, of, tap, throwError } from 'rxjs';
import { ApiClient } from '../../core/api-client';
import { SessaoService } from '../../core/sessao.service';
import { NotificacaoUiService } from '../../core/notificacao-ui.service';
import { ErroApi } from '../../core/erro-api';
import {
  Credenciais,
  DadosRegisto,
  RespostaMensagem,
  SessaoAtual,
  TokenAcesso,
} from '../../shared/models/sessao';

@Injectable({ providedIn: 'root' })
export class AutenticacaoApiService {
  private readonly api = inject(ApiClient);
  private readonly sessao = inject(SessaoService);
  private readonly ui = inject(NotificacaoUiService);
  registar(dados: DadosRegisto) {
    return this.api.post<RespostaMensagem>('autenticacao/registo', dados, { erroNaPagina: true });
  }
  reenviarConfirmacao(email: string) {
    return this.api.post<RespostaMensagem>(
      'autenticacao/reenviar-confirmacao',
      { email },
      { erroNaPagina: true },
    );
  }
  confirmar(token: string) {
    return this.api.post<void>('autenticacao/confirmar', { token }, { erroNaPagina: true });
  }
  login(dados: Credenciais) {
    return this.api.post<TokenAcesso>('autenticacao/login', dados, { erroNaPagina: true }).pipe(
      tap((token) => this.sessao.iniciar(token)),
      catchError((erro: unknown) => {
        this.sessao.terminar();
        const falha =
          erro instanceof ErroApi
            ? erro
            : new ErroApi(
                0,
                'Não foi possível guardar a sessão. Verifica se o armazenamento do browser está disponível e tenta novamente.',
              );
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
  /**
   * Pede a revogação do token e termina sempre a sessão local (UC-F001-05 E1).
   * Emite `false` quando o servidor já não reconhecia a sessão (UC-F001-05 A1).
   */
  logout() {
    return this.api.post<void>('autenticacao/logout').pipe(
      map(() => {
        this.ui.sucesso('Sessão terminada.', { manterAoNavegar: true });
        return true;
      }),
      catchError((erro: unknown) => {
        const jaTerminada = erro instanceof ErroApi && erro.status === 401;
        if (jaTerminada)
          this.ui.sucesso('A sessão já não estava ativa.', { manterAoNavegar: true });
        else
          this.ui.erro(
            'A sessão foi terminada neste browser, mas o servidor não confirmou a revogação.',
            { manterAoNavegar: true },
          );
        return of(!jaTerminada);
      }),
      // Antes de emitir: quem subscreve pode navegar logo para uma rota só de visitantes.
      tap(() => this.sessao.terminar()),
    );
  }
}
