import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';
import { CONFIGURACAO } from '../../core/configuracao';
import { autenticacaoInterceptor, erroInterceptor } from '../../core/interceptors';
import { NotificacaoUiService } from '../../core/notificacao-ui.service';
import { SessaoService } from '../../core/sessao.service';
import { ConfirmacaoPage } from './confirmacao.page';

describe('I070 — confirmação da conta', () => {
  const base = 'http://localhost:5100/api/v1';
  const urlConfirmar = base + '/autenticacao/confirmar';
  const ligacao = '/confirmar-conta?token=abc123';
  const SUCESSO = { status: 204, statusText: 'No Content' };
  let http: HttpTestingController;

  async function abrir(url = ligacao) {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url, ConfirmacaoPage);
    return { harness, raiz: harness.routeNativeElement as HTMLElement };
  }

  async function concluirNavegacao(harness: RouterTestingHarness) {
    await harness.fixture.whenStable();
    return TestBed.inject(Router).url;
  }

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        { provide: CONFIGURACAO, useValue: { apiBaseUrl: base } },
        provideRouter(routes),
        provideHttpClient(withInterceptors([autenticacaoInterceptor, erroInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
    sessionStorage.clear();
  });

  it('envia o token da ligação e mostra que está a confirmar', async () => {
    const { harness, raiz } = await abrir();
    harness.detectChanges();
    const pedido = http.expectOne(urlConfirmar);
    expect(pedido.request.method).toBe('POST');
    expect(pedido.request.body).toEqual({ token: 'abc123' });
    expect(pedido.request.headers.has('Authorization')).toBeFalse();
    expect(raiz.querySelector('h1')?.textContent).toContain('A confirmar');
    pedido.flush(null, SUCESSO);
  });

  it('204 leva à página de entrada e avisa que a conta ficou confirmada (RF-F001-07)', async () => {
    const { harness } = await abrir();
    http.expectOne(urlConfirmar).flush(null, SUCESSO);
    expect(await concluirNavegacao(harness)).toBe('/entrar');
    expect(TestBed.inject(NotificacaoUiService).mensagem()).toEqual({
      tipo: 'sucesso',
      texto: 'Conta confirmada. Já podes entrar.',
    });
  });

  it('confirma mesmo com outra sessão aberta, sem enviar o token dessa sessão', async () => {
    TestBed.inject(SessaoService).iniciar({
      accessToken: `a.${btoa(JSON.stringify({ sub: 'aluno-1', exp: Math.floor(Date.now() / 1000) + 3600 }))}.c`,
      expiresIn: 3600,
    });
    const { harness } = await abrir();
    const pedido = http.expectOne(urlConfirmar);
    expect(pedido.request.headers.has('Authorization')).toBeFalse();
    pedido.flush(null, SUCESSO);
    expect(await concluirNavegacao(harness)).toBe('/inicio');
  });

  describe('ligação recusada (RF-F001-08)', () => {
    for (const [status, mensagem] of [
      [400, 'Esta ligação expirou. Pede uma nova ligação de confirmação.'],
      [400, 'Esta ligação já foi usada. Se a conta está confirmada, inicia sessão.'],
      [400, 'Ligação de confirmação inválida. Pede uma nova ligação.'],
      [404, 'A conta desta ligação já não existe.'],
    ] as const) {
      it(`${status} «${mensagem}» explica o motivo e oferece o reenvio e a entrada`, async () => {
        const { harness, raiz } = await abrir();
        http.expectOne(urlConfirmar).flush({ erro: mensagem }, { status, statusText: 'Erro' });
        harness.detectChanges();
        expect(raiz.querySelector('[role="alert"]')?.textContent).toContain(mensagem);
        expect(raiz.querySelector('a[href="/reenviar-confirmacao"]')).not.toBeNull();
        expect(raiz.querySelector('a[href="/entrar"]')).not.toBeNull();
        expect(raiz.querySelector('button')).toBeNull();
        expect(TestBed.inject(Router).url).toBe(ligacao);
        expect(TestBed.inject(NotificacaoUiService).mensagem()).toBeNull();
      });
    }

    for (const url of ['/confirmar-conta', '/confirmar-conta?token=%20']) {
      it(`${url} não chama a API e oferece o reenvio`, async () => {
        const { harness, raiz } = await abrir(url);
        harness.detectChanges();
        http.expectNone(urlConfirmar);
        expect(raiz.querySelector('[role="alert"]')?.textContent).toContain('não é válida');
        expect(raiz.querySelector('a[href="/reenviar-confirmacao"]')).not.toBeNull();
      });
    }
  });

  describe('falha temporária', () => {
    const casos: [
      string,
      (pedido: ReturnType<HttpTestingController['expectOne']>) => void,
      string,
    ][] = [
      [
        'sem ligação ao servidor',
        (pedido) => pedido.error(new ProgressEvent('error')),
        'Não foi possível contactar o servidor',
      ],
      [
        '429',
        (pedido) => pedido.flush(null, { status: 429, statusText: 'Too Many Requests' }),
        'demasiados pedidos',
      ],
      [
        '503 da infraestrutura',
        (pedido) =>
          pedido.flush('<html></html>', { status: 503, statusText: 'Service Unavailable' }),
        'temporariamente indisponível',
      ],
    ];

    for (const [nome, falhar, texto] of casos) {
      it(`${nome}: mostra o motivo e não oferece o reenvio, porque a ligação continua válida`, async () => {
        const { harness, raiz } = await abrir();
        falhar(http.expectOne(urlConfirmar));
        harness.detectChanges();
        expect(raiz.querySelector('[role="alert"]')?.textContent).toContain(texto);
        expect(raiz.querySelector('button')?.textContent).toContain('Tentar novamente');
        expect(raiz.querySelector('a[href="/reenviar-confirmacao"]')).toBeNull();
        expect(TestBed.inject(NotificacaoUiService).mensagem()).toBeNull();
      });
    }

    it('tentar novamente repete o pedido com o mesmo token e conclui a confirmação', async () => {
      const { harness, raiz } = await abrir();
      http.expectOne(urlConfirmar).error(new ProgressEvent('error'));
      harness.detectChanges();
      raiz.querySelector('button')?.click();
      harness.detectChanges();
      expect(raiz.querySelector('h1')?.textContent).toContain('A confirmar');
      expect(raiz.querySelector('button')).toBeNull();
      const repetido = http.expectOne(urlConfirmar);
      expect(repetido.request.body).toEqual({ token: 'abc123' });
      repetido.flush(null, SUCESSO);
      expect(await concluirNavegacao(harness)).toBe('/entrar');
    });
  });
});
