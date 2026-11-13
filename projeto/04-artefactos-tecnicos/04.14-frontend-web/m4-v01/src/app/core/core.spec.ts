import { TestBed } from '@angular/core/testing';
import {
  HttpClient,
  HttpErrorResponse,
  HttpParams,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  carregarConfiguracao,
  CONFIGURACAO,
  pertenceApi,
  validarConfiguracao,
} from './configuracao';
import { ApiClient } from './api-client';
import { SessaoService } from './sessao.service';
import { NotificacaoUiService } from './notificacao-ui.service';
import { autenticacaoInterceptor, erroInterceptor } from './interceptors';
import { normalizarErro } from './erro-api';
import { destinoInterno } from './guards';
import { AutenticacaoApiService } from '../features/autenticacao/autenticacao-api.service';

describe('I069 — núcleo', () => {
  const config = { apiBaseUrl: 'https://api.example.test/api/v1' };
  const chave = `campus-carpool:sessao:${config.apiBaseUrl}`;
  function token(segundos = 3600, id = 'aluno-1') {
    return `eyJhbGciOiJIUzI1NiJ9.${btoa(JSON.stringify({ sub: id, exp: Math.floor(Date.now() / 1000) + segundos }))}.assinatura`;
  }
  function setup() {
    TestBed.configureTestingModule({
      providers: [
        { provide: CONFIGURACAO, useValue: config },
        provideRouter([]),
        provideHttpClient(withInterceptors([autenticacaoInterceptor, erroInterceptor])),
        provideHttpClientTesting(),
      ],
    });
  }
  beforeEach(() => {
    sessionStorage.clear();
    setup();
  });
  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.resetTestingModule();
    sessionStorage.clear();
  });

  describe('Configuração em execução', () => {
    it('aceita API pública e localhost, normalizando barra final', () => {
      expect(validarConfiguracao({ apiBaseUrl: config.apiBaseUrl + '/' })).toEqual(config);
      expect(
        validarConfiguracao({ apiBaseUrl: 'http://localhost:5100/api/v1' }).apiBaseUrl,
      ).toContain('localhost');
    });
    it('recusa segredos no URL, HTTP remoto, caminhos e configurações inválidas', () => {
      for (const apiBaseUrl of [
        'http://publico.test/api/v1',
        'https://u:p@x.test/api/v1',
        'https://x.test/api/v1?token=x',
        'https://x.test/api/v1#x',
        'https://x.test/',
        'not-a-url',
      ])
        expect(() => validarConfiguracao({ apiBaseUrl })).toThrow();
      for (const valor of [null, {}, { apiBaseUrl: 12 }])
        expect(() => validarConfiguracao(valor)).toThrow();
    });
    it('não confunde uma origem ou um prefixo semelhante com a API', () => {
      expect(pertenceApi(config.apiBaseUrl, config)).toBeTrue();
      expect(pertenceApi(config.apiBaseUrl + '/utilizadores/me', config)).toBeTrue();
      expect(pertenceApi(config.apiBaseUrl + '-roubar', config)).toBeFalse();
      expect(pertenceApi('https://api.example.test.evil/api/v1/me', config)).toBeFalse();
    });
    it('carrega o ficheiro sem cache e propaga falhas de carregamento', async () => {
      const fetchSpy = spyOn(window, 'fetch').and.resolveTo(new Response(JSON.stringify(config)));
      expect(await carregarConfiguracao()).toEqual(config);
      expect(fetchSpy.calls.mostRecent().args[1]).toEqual({ cache: 'no-store' });
      fetchSpy.and.resolveTo(new Response('', { status: 404 }));
      await expectAsync(carregarConfiguracao()).toBeRejected();
    });
  });

  describe('Sessão', () => {
    it('guarda em sessionStorage e recupera numa nova instância', () => {
      const jwt = token();
      const s = TestBed.inject(SessaoService);
      s.iniciar({ accessToken: jwt, expiresIn: 3600 });
      expect(s.obterToken()).toBe(jwt);
      expect(s.utilizador()?.id).toBe('aluno-1');
      TestBed.resetTestingModule();
      setup();
      expect(TestBed.inject(SessaoService).obterToken()).toBe(jwt);
    });
    it('remove dados corrompidos ou expirados sem impedir o arranque', () => {
      sessionStorage.setItem(chave, '{');
      expect(TestBed.inject(SessaoService).autenticado()).toBeFalse();
      expect(sessionStorage.getItem(chave)).toBeNull();
      TestBed.resetTestingModule();
      setup();
      sessionStorage.setItem(
        chave,
        JSON.stringify({
          accessToken: token(-1),
          expiraEm: Date.now() - 1000,
          utilizador: { id: 'aluno-1' },
        }),
      );
      expect(TestBed.inject(SessaoService).obterToken()).toBeNull();
    });
    it('não reutiliza sessão ao mudar de backend', () => {
      TestBed.inject(SessaoService).iniciar({ accessToken: token(), expiresIn: 3600 });
      TestBed.resetTestingModule();
      setup();
      TestBed.overrideProvider(CONFIGURACAO, {
        useValue: { apiBaseUrl: 'https://outra.test/api/v1' },
      });
      expect(TestBed.inject(SessaoService).obterToken()).toBeNull();
    });
    it('expira automaticamente e limpa token e identidade', () => {
      jasmine.clock().install();
      jasmine.clock().mockDate(new Date('2026-11-08T12:00:00Z'));
      try {
        const s = TestBed.inject(SessaoService);
        s.iniciar({ accessToken: token(5), expiresIn: 5 });
        jasmine.clock().tick(5001);
        expect(s.autenticado()).toBeFalse();
        expect(s.utilizador()).toBeNull();
        expect(sessionStorage.getItem(chave)).toBeNull();
      } finally {
        jasmine.clock().uninstall();
      }
    });
    it('recusa tokens malformados, expirados e validades inválidas', () => {
      const s = TestBed.inject(SessaoService);
      for (const accessToken of ['x', 'a.e30.c', token(-1)])
        expect(() => s.iniciar({ accessToken, expiresIn: 3600 })).toThrow();
      expect(() => s.iniciar({ accessToken: token(), expiresIn: 0 })).toThrow();
      expect(s.autenticado()).toBeFalse();
    });
    it('não considera a sessão iniciada se não a conseguir guardar', () => {
      const s = TestBed.inject(SessaoService);
      spyOn(Storage.prototype, 'setItem').and.throwError('bloqueado');
      expect(() => s.iniciar({ accessToken: token(), expiresIn: 3600 })).toThrow();
      expect(s.autenticado()).toBeFalse();
    });
    it('limpa memória mesmo se o armazenamento falhar', () => {
      const s = TestBed.inject(SessaoService);
      s.iniciar({ accessToken: token(), expiresIn: 3600 });
      spyOn(Storage.prototype, 'removeItem').and.throwError('bloqueado');
      s.terminar();
      expect(s.obterToken()).toBeNull();
    });
  });

  describe('Cliente HTTP e autenticação', () => {
    it('usa o endereço configurado, verbos e parâmetros sem revelar o token a terceiros', () => {
      const api = TestBed.inject(ApiClient),
        http = TestBed.inject(HttpTestingController),
        jwt = token();
      TestBed.inject(SessaoService).iniciar({ accessToken: jwt, expiresIn: 3600 });
      api.get('viagens', new HttpParams().set('origem', 'Braga')).subscribe();
      const get = http.expectOne(config.apiBaseUrl + '/viagens?origem=Braga');
      expect(get.request.headers.get('Authorization')).toBe('Bearer ' + jwt);
      get.flush([]);
      api.put('utilizadores/me', { nome: 'Ana' }).subscribe();
      const put = http.expectOne(config.apiBaseUrl + '/utilizadores/me');
      expect(put.request.method).toBe('PUT');
      put.flush({});
      api.delete('utilizadores/me/ligacoes/1').subscribe();
      const del = http.expectOne(config.apiBaseUrl + '/utilizadores/me/ligacoes/1');
      expect(del.request.method).toBe('DELETE');
      del.flush(null);
      TestBed.inject(HttpClient).get('https://externo.test/recurso').subscribe();
      const externo = http.expectOne('https://externo.test/recurso');
      expect(externo.request.headers.has('Authorization')).toBeFalse();
      externo.flush({});
      expect(() => api.get('https://externo.test')).toThrow();
      expect(() => api.get('../segredo')).toThrow();
    });
    it('faz login com o contrato atual e inicia a sessão com a resposta do servidor', async () => {
      const auth = TestBed.inject(AutenticacaoApiService),
        http = TestBed.inject(HttpTestingController),
        jwt = token();
      const p = firstValueFrom(auth.login({ email: 'ana@alunos.test', password: 'teste' }));
      const login = http.expectOne(config.apiBaseUrl + '/autenticacao/login');
      expect(login.request.body).toEqual({ email: 'ana@alunos.test', password: 'teste' });
      expect(login.request.headers.has('Authorization')).toBeFalse();
      login.flush({ accessToken: jwt, expiresIn: 3600 });
      await p;
      expect(TestBed.inject(SessaoService).obterToken()).toBe(jwt);
    });
    it('limpa a sessão restaurada se a identidade do servidor for diferente', async () => {
      TestBed.inject(SessaoService).iniciar({ accessToken: token(), expiresIn: 3600 });
      const p = firstValueFrom(TestBed.inject(AutenticacaoApiService).validar());
      TestBed.inject(HttpTestingController)
        .expectOne(config.apiBaseUrl + '/autenticacao/me')
        .flush({ id: 'outro' });
      await expectAsync(p).toBeRejectedWithError(
        'Não foi possível validar a sessão. Inicia sessão novamente.',
      );
      expect(TestBed.inject(SessaoService).autenticado()).toBeFalse();
    });
    it('recusa resposta de login malformada sem apresentar detalhes internos', async () => {
      const p = firstValueFrom(
        TestBed.inject(AutenticacaoApiService).login({ email: 'a@b.test', password: 'x' }),
      );
      TestBed.inject(HttpTestingController)
        .expectOne(config.apiBaseUrl + '/autenticacao/login')
        .flush({ accessToken: 'invalid', expiresIn: 3600 });
      await expectAsync(p).toBeRejectedWithError(/Não foi possível guardar a sessão/);
      expect(TestBed.inject(SessaoService).autenticado()).toBeFalse();
    });
    it('só a página apresenta o erro de um pedido marcado com erroNaPagina', async () => {
      const api = TestBed.inject(ApiClient),
        http = TestBed.inject(HttpTestingController),
        ui = TestBed.inject(NotificacaoUiService),
        conflito = { status: 409, statusText: 'Conflict' };
      const naPagina = firstValueFrom(api.post('viagens', {}, { erroNaPagina: true }));
      http.expectOne(config.apiBaseUrl + '/viagens').flush({ erro: 'Conflito.' }, conflito);
      await expectAsync(naPagina).toBeRejectedWithError('Conflito.');
      expect(ui.mensagem()).toBeNull();
      const global = firstValueFrom(api.post('viagens', {}));
      http.expectOne(config.apiBaseUrl + '/viagens').flush({ erro: 'Conflito.' }, conflito);
      await expectAsync(global).toBeRejectedWithError('Conflito.');
      expect(ui.mensagem()?.texto).toBe('Conflito.');
    });
    it('apresenta credenciais inválidas sem redirecionar repetidamente', async () => {
      const navegar = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
      const p = firstValueFrom(
        TestBed.inject(AutenticacaoApiService).login({ email: 'a@b.test', password: 'x' }),
      );
      TestBed.inject(HttpTestingController)
        .expectOne(config.apiBaseUrl + '/autenticacao/login')
        .flush({ erro: 'Credenciais inválidas.' }, { status: 401, statusText: 'Unauthorized' });
      await expectAsync(p).toBeRejectedWithError('Credenciais inválidas.');
      expect(navegar).not.toHaveBeenCalled();
      expect(TestBed.inject(NotificacaoUiService).mensagem()).toBeNull();
    });
    it('em 401 protegido remove a sessão e envia para entrar', async () => {
      const s = TestBed.inject(SessaoService);
      s.iniciar({ accessToken: token(), expiresIn: 3600 });
      const navegar = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
      const p = firstValueFrom(TestBed.inject(ApiClient).get('utilizadores/me'));
      TestBed.inject(HttpTestingController)
        .expectOne(config.apiBaseUrl + '/utilizadores/me')
        .flush(null, { status: 401, statusText: 'Unauthorized' });
      await expectAsync(p).toBeRejected();
      expect(s.autenticado()).toBeFalse();
      expect(navegar).toHaveBeenCalledWith(['/entrar']);
    });
    it('um 401 antigo não termina a sessão entretanto renovada por novo login', async () => {
      const s = TestBed.inject(SessaoService);
      s.iniciar({ accessToken: token(300), expiresIn: 300 });
      const p = firstValueFrom(TestBed.inject(ApiClient).get('utilizadores/me'));
      const req = TestBed.inject(HttpTestingController).expectOne(
        config.apiBaseUrl + '/utilizadores/me',
      );
      s.iniciar({ accessToken: token(3600), expiresIn: 3600 });
      req.flush(null, { status: 401, statusText: 'Unauthorized' });
      await expectAsync(p).toBeRejected();
      expect(s.autenticado()).toBeTrue();
      expect(TestBed.inject(NotificacaoUiService).mensagem()).toBeNull();
    });
    it('não aplica tratamento de sessão a pedidos externos', async () => {
      const s = TestBed.inject(SessaoService);
      s.iniciar({ accessToken: token(), expiresIn: 3600 });
      const p = firstValueFrom(TestBed.inject(HttpClient).get('https://externo.test'));
      TestBed.inject(HttpTestingController)
        .expectOne('https://externo.test')
        .flush(null, { status: 401, statusText: 'Unauthorized' });
      await expectAsync(p).toBeRejected();
      expect(s.autenticado()).toBeTrue();
      expect(TestBed.inject(NotificacaoUiService).mensagem()).toBeNull();
    });
    it('revoga no servidor antes de limpar o armazenamento no logout', async () => {
      const s = TestBed.inject(SessaoService),
        jwt = token();
      s.iniciar({ accessToken: jwt, expiresIn: 3600 });
      const p = firstValueFrom(TestBed.inject(AutenticacaoApiService).logout());
      const req = TestBed.inject(HttpTestingController).expectOne(
        config.apiBaseUrl + '/autenticacao/logout',
      );
      expect(req.request.body).toBeNull();
      expect(req.request.headers.get('Authorization')).toBe('Bearer ' + jwt);
      expect(s.autenticado()).toBeTrue();
      req.flush(null, { status: 204, statusText: 'No Content' });
      await p;
      expect(s.autenticado()).toBeFalse();
      expect(sessionStorage.getItem(chave)).toBeNull();
    });
    it('termina localmente quando o logout não consegue contactar o servidor e informa a limitação', async () => {
      const s = TestBed.inject(SessaoService);
      s.iniciar({ accessToken: token(), expiresIn: 3600 });
      const p = firstValueFrom(TestBed.inject(AutenticacaoApiService).logout());
      TestBed.inject(HttpTestingController)
        .expectOne(config.apiBaseUrl + '/autenticacao/logout')
        .error(new ProgressEvent('error'));
      await p;
      expect(s.autenticado()).toBeFalse();
      expect(TestBed.inject(NotificacaoUiService).mensagem()?.texto).toContain(
        'não confirmou a revogação',
      );
    });
    it('aceita um logout de uma sessão que o servidor já invalidou', async () => {
      spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
      TestBed.inject(SessaoService).iniciar({ accessToken: token(), expiresIn: 3600 });
      const p = firstValueFrom(TestBed.inject(AutenticacaoApiService).logout());
      TestBed.inject(HttpTestingController)
        .expectOne(config.apiBaseUrl + '/autenticacao/logout')
        .flush(null, { status: 401, statusText: 'Unauthorized' });
      await p;
      expect(TestBed.inject(NotificacaoUiService).mensagem()?.texto).toBe(
        'A sessão já não estava ativa.',
      );
    });
  });

  describe('Erros e navegação segura', () => {
    it('interpreta CorpoErro, formato antigo e ProblemDetails/validação', () => {
      expect(
        normalizarErro(new HttpErrorResponse({ status: 409, error: { erro: 'Conflito.' } }))
          .message,
      ).toBe('Conflito.');
      const e = normalizarErro(
        new HttpErrorResponse({
          status: 400,
          error: {
            erro: {
              mensagem: 'Dados inválidos.',
              campos: { email: ['Inválido'], nome: 'Obrigatório', outro: 12 },
            },
          },
        }),
      );
      expect(e.campos).toEqual({ email: ['Inválido'], nome: ['Obrigatório'] });
      expect(
        normalizarErro(
          new HttpErrorResponse({
            status: 400,
            error: { detail: 'Inválido.', errors: { Nome: ['Falta nome'] } },
          }),
        ).campos['Nome'],
      ).toEqual(['Falta nome']);
    });
    it('não mostra stack traces, HTML ou detalhes de erros 5xx', () => {
      for (const status of [0, 400, 401, 403, 404, 409, 429, 500, 503]) {
        expect(
          normalizarErro(new HttpErrorResponse({ status, error: '<html>segredo</html>' })).message,
        ).not.toContain('segredo');
      }
      expect(
        normalizarErro(
          new HttpErrorResponse({ status: 500, error: { erro: 'stacktrace', detail: 'password' } }),
        ).message,
      ).not.toContain('stacktrace');
    });
    it('mostra a mensagem da API nas falhas de serviços externos', () => {
      for (const status of [502, 503, 504])
        expect(
          normalizarErro(new HttpErrorResponse({ status, error: { erro: 'Conta criada.' } }))
            .message,
        ).toBe('Conta criada.');
    });
    it('indica se a mensagem veio da API ou é a alternativa do cliente', () => {
      const daApi = new HttpErrorResponse({ status: 503, error: { erro: 'Conta criada.' } });
      const semCorpo = new HttpErrorResponse({ status: 503, error: '<html></html>' });
      expect(normalizarErro(daApi).mensagemDaApi).toBeTrue();
      expect(normalizarErro(semCorpo).mensagemDaApi).toBeFalse();
    });
    it('aceita apenas destinos internos após login', () => {
      for (const destino of ['/perfil', '/inicio', '/utilizadores/1?origem=viagem'])
        expect(destinoInterno(destino)).toBe(destino);
      for (const destino of [
        null,
        '',
        '//evil.test',
        '/\\evil.test',
        'https://evil.test',
        '/entrar',
        '/entrar?regresso=%2Fperfil',
      ])
        expect(destinoInterno(destino)).toBe('/inicio');
    });
    it('permite limpar uma notificação', () => {
      const ui = TestBed.inject(NotificacaoUiService);
      ui.sucesso('Feito');
      expect(ui.mensagem()?.tipo).toBe('sucesso');
      ui.limpar();
      expect(ui.mensagem()).toBeNull();
    });
  });
});
