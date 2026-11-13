import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
  TestRequest,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';
import { CONFIGURACAO } from '../../core/configuracao';
import { autenticacaoInterceptor, erroInterceptor } from '../../core/interceptors';
import { NotificacaoUiService } from '../../core/notificacao-ui.service';
import { SessaoService } from '../../core/sessao.service';
import { InicioPage } from '../inicio/inicio.page';
import { LoginPage } from './login.page';

describe('I070 — início de sessão', () => {
  const base = 'http://localhost:5100/api/v1';
  const urlLogin = base + '/autenticacao/login';
  const EMAIL = 'ana@estg.ipp.pt';
  const LIGACAO_REENVIO = 'a[href="/reenviar-confirmacao"]';
  const tokenValido = () => ({
    accessToken: `a.${btoa(JSON.stringify({ sub: 'aluno-1', exp: Math.floor(Date.now() / 1000) + 3600 }))}.c`,
    expiresIn: 3600,
  });
  let http: HttpTestingController;

  async function abrir(url = '/entrar') {
    const harness = await RouterTestingHarness.create();
    const pagina = await harness.navigateByUrl(url, LoginPage);
    return { harness, pagina, raiz: harness.routeNativeElement as HTMLElement };
  }
  function submeter(pagina: LoginPage) {
    pagina.formulario.setValue({ email: EMAIL, password: 'Palavra1' });
    pagina.entrar();
  }
  const recusar = (status: number, erro: string) =>
    http.expectOne(urlLogin).flush({ erro }, { status, statusText: 'Erro' });

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

  describe('com credenciais corretas', () => {
    it('envia o email e a palavra-passe, inicia a sessão e mostra a área principal (RF-F001-10)', async () => {
      const { harness, pagina } = await abrir();
      submeter(pagina);
      const pedido = http.expectOne(urlLogin);
      expect(pedido.request.method).toBe('POST');
      expect(pedido.request.body).toEqual({ email: EMAIL, password: 'Palavra1' });
      pedido.flush(tokenValido());
      await harness.fixture.whenStable();
      expect(TestBed.inject(SessaoService).autenticado()).toBeTrue();
      expect(TestBed.inject(Router).url).toBe('/inicio');
    });

    for (const [regresso, destino] of [
      ['%2Finicio%3Forigem%3Dviagem', '/inicio?origem=viagem'],
      ['https%3A%2F%2Fevil.test', '/inicio'],
    ] as const) {
      it(`com regresso=${regresso} segue para ${destino}`, async () => {
        const { harness, pagina } = await abrir(`/entrar?regresso=${regresso}`);
        submeter(pagina);
        http.expectOne(urlLogin).flush(tokenValido());
        await harness.fixture.whenStable();
        expect(TestBed.inject(Router).url).toBe(destino);
      });
    }

    it('apaga a notificação anterior ao submeter', async () => {
      const { pagina } = await abrir();
      const ui = TestBed.inject(NotificacaoUiService);
      ui.sucesso('Conta confirmada. Já podes entrar.');
      submeter(pagina);
      expect(ui.mensagem()).toBeNull();
      recusar(401, 'Credenciais inválidas.');
    });
  });

  describe('credenciais recusadas (401)', () => {
    it('mostra só a mensagem genérica da API, sem indicar o campo errado (RF-F001-11)', async () => {
      const { harness, pagina, raiz } = await abrir();
      submeter(pagina);
      recusar(401, 'Credenciais inválidas.');
      harness.detectChanges();
      expect(raiz.querySelector('form [role="alert"]')?.textContent).toContain(
        'Credenciais inválidas.',
      );
      expect(raiz.querySelector('#email')?.getAttribute('aria-invalid')).toBe('false');
      expect(raiz.querySelector('#password')?.getAttribute('aria-invalid')).toBe('false');
      expect(TestBed.inject(NotificacaoUiService).mensagem()).toBeNull();
    });

    it('não cria sessão, limpa a palavra-passe e fica na página de entrada', async () => {
      const { harness, pagina } = await abrir();
      submeter(pagina);
      recusar(401, 'Credenciais inválidas.');
      harness.detectChanges();
      expect(TestBed.inject(SessaoService).autenticado()).toBeFalse();
      expect(pagina.formulario.controls.password.value).toBe('');
      expect(pagina.formulario.controls.email.value).toBe(EMAIL);
      expect(pagina.aEntrar()).toBeFalse();
      expect(TestBed.inject(Router).url).toBe('/entrar');
    });

    it('não oferece o reenvio em destaque', async () => {
      const { harness, pagina, raiz } = await abrir();
      submeter(pagina);
      recusar(401, 'Credenciais inválidas.');
      harness.detectChanges();
      expect(raiz.querySelector('a.button[href="/reenviar-confirmacao"]')).toBeNull();
      expect(raiz.querySelectorAll(LIGACAO_REENVIO).length).toBe(1);
    });
  });

  describe('conta por confirmar (403, UC-F001-04 E2)', () => {
    it('diz que falta confirmar o email e não cria sessão', async () => {
      const { harness, pagina, raiz } = await abrir();
      submeter(pagina);
      recusar(403, 'Conta por confirmar.');
      harness.detectChanges();
      expect(raiz.querySelector('.notice.error')?.textContent).toContain('Conta por confirmar.');
      expect(raiz.textContent).toContain('confirma primeiro o teu email');
      expect(raiz.querySelector('form [role="alert"]')).toBeNull();
      expect(TestBed.inject(SessaoService).autenticado()).toBeFalse();
      expect(TestBed.inject(NotificacaoUiService).mensagem()).toBeNull();
    });

    it('oferece uma única ligação de reenvio, com o email usado, fora do URL', async () => {
      const { harness, pagina, raiz } = await abrir();
      submeter(pagina);
      recusar(403, 'Conta por confirmar.');
      harness.detectChanges();
      const ligacoes = raiz.querySelectorAll<HTMLAnchorElement>(LIGACAO_REENVIO);
      expect(ligacoes.length).toBe(1);
      ligacoes[0].click();
      await harness.fixture.whenStable();
      harness.detectChanges();
      expect(TestBed.inject(Router).url).toBe('/reenviar-confirmacao');
      expect((harness.routeNativeElement as HTMLElement).querySelector('input')?.value).toBe(EMAIL);
    });

    it('retira o destaque ao tentar de novo', async () => {
      const { harness, pagina, raiz } = await abrir();
      submeter(pagina);
      recusar(403, 'Conta por confirmar.');
      harness.detectChanges();
      submeter(pagina);
      harness.detectChanges();
      expect(raiz.querySelector('.notice.error')).toBeNull();
      expect(raiz.querySelector('a.button[href="/reenviar-confirmacao"]')).toBeNull();
      recusar(401, 'Credenciais inválidas.');
    });
  });

  describe('outras falhas', () => {
    for (const [nome, responder, texto] of [
      [
        '429',
        (r: TestRequest) => r.flush(null, { status: 429, statusText: 'Too Many' }),
        'demasiados pedidos',
      ],
      [
        'sem ligação',
        (r: TestRequest) => r.error(new ProgressEvent('error')),
        'Não foi possível contactar o servidor',
      ],
    ] as const) {
      it(`${nome} mostra o erro só junto do formulário`, async () => {
        const { harness, pagina, raiz } = await abrir();
        submeter(pagina);
        responder(http.expectOne(urlLogin));
        harness.detectChanges();
        expect(raiz.querySelector('form [role="alert"]')?.textContent).toContain(texto);
        expect(raiz.querySelector('a.button[href="/reenviar-confirmacao"]')).toBeNull();
        expect(TestBed.inject(NotificacaoUiService).mensagem()).toBeNull();
        expect(TestBed.inject(Router).url).toBe('/entrar');
      });
    }

    it('limpa o erro anterior ao tentar de novo', async () => {
      const { harness, pagina, raiz } = await abrir();
      submeter(pagina);
      recusar(401, 'Credenciais inválidas.');
      harness.detectChanges();
      submeter(pagina);
      harness.detectChanges();
      expect(raiz.querySelector('[role="alert"]')).toBeNull();
      http.expectOne(urlLogin).flush(tokenValido());
      await harness.fixture.whenStable();
    });
  });

  describe('formulário', () => {
    for (const [email, password, mensagens] of [
      ['', '', ['Indica o teu email académico.', 'Indica a tua palavra-passe.']],
      ['sem-arroba', 'Palavra1', ['Indica um email válido.']],
    ] as const) {
      it(`recusa email «${email}» sem chamar a API`, async () => {
        const { harness, pagina, raiz } = await abrir();
        pagina.formulario.setValue({ email, password });
        pagina.entrar();
        harness.detectChanges();
        http.expectNone(urlLogin);
        for (const mensagem of mensagens) expect(raiz.textContent).toContain(mensagem);
        expect(raiz.querySelector('#email')?.getAttribute('aria-invalid')).toBe('true');
      });
    }

    it('não permite pedidos repetidos enquanto o primeiro decorre', async () => {
      const { harness, pagina, raiz } = await abrir();
      submeter(pagina);
      pagina.entrar();
      harness.detectChanges();
      const botao = raiz.querySelector<HTMLButtonElement>('button[type="submit"]');
      expect(botao?.disabled).toBeTrue();
      expect(botao?.textContent).toContain('A entrar');
      recusar(401, 'Credenciais inválidas.');
    });

    it('a ligação de reenvio leva o email escrito', async () => {
      const { harness, pagina, raiz } = await abrir();
      pagina.formulario.controls.email.setValue(EMAIL);
      harness.detectChanges();
      raiz.querySelector<HTMLAnchorElement>(LIGACAO_REENVIO)?.click();
      await harness.fixture.whenStable();
      harness.detectChanges();
      expect((harness.routeNativeElement as HTMLElement).querySelector('input')?.value).toBe(EMAIL);
    });
  });

  it('quem tem sessão não vê esta página e volta ao início', async () => {
    TestBed.inject(SessaoService).iniciar(tokenValido());
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/entrar', InicioPage);
    expect(TestBed.inject(Router).url).toBe('/inicio');
  });
});
