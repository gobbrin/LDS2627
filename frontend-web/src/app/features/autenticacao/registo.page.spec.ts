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
import { InicioPage } from '../inicio/inicio.page';
import { LoginPage } from './login.page';
import { RegistoPage } from './registo.page';

describe('I070 — registo de conta', () => {
  const base = 'http://localhost:5100/api/v1';
  const urlRegisto = base + '/autenticacao/registo';
  const validos = {
    nome: '  Ana Silva ',
    email: 'ana@estg.ipp.pt',
    password: 'Palavra1',
    confirmacao: 'Palavra1',
  };
  let http: HttpTestingController;

  async function abrir() {
    const harness = await RouterTestingHarness.create();
    const pagina = await harness.navigateByUrl('/registo', RegistoPage);
    const raiz = harness.routeNativeElement as HTMLElement;
    return { harness, pagina, raiz };
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

  describe('sucesso', () => {
    it('envia os dados limpos, sem a confirmação da palavra-passe', async () => {
      const { pagina } = await abrir();
      pagina.formulario.setValue(validos);
      pagina.registar();
      const pedido = http.expectOne(urlRegisto);
      expect(pedido.request.method).toBe('POST');
      expect(pedido.request.body).toEqual({
        nome: 'Ana Silva',
        email: 'ana@estg.ipp.pt',
        password: 'Palavra1',
      });
      pedido.flush({ mensagem: 'ok' }, { status: 202, statusText: 'Accepted' });
    });
    it('202 pede para verificar o email, esconde o formulário e esquece a palavra-passe', async () => {
      const { harness, pagina, raiz } = await abrir();
      pagina.formulario.setValue(validos);
      pagina.registar();
      http
        .expectOne(urlRegisto)
        .flush(
          { mensagem: 'Verifica o email académico para confirmar a conta.' },
          { status: 202, statusText: 'Accepted' },
        );
      harness.detectChanges();
      expect(raiz.querySelector('h1')?.textContent).toContain('Verifica o teu email');
      expect(raiz.querySelector('[role="status"]')?.textContent).toContain(
        'Verifica o email académico',
      );
      expect(raiz.textContent).toContain('ana@estg.ipp.pt');
      expect(raiz.querySelector('form')).toBeNull();
      expect(raiz.querySelector('a[href="/entrar"]')).not.toBeNull();
      expect(pagina.formulario.controls.password.value).toBe('');
    });
    it('não permite submissões repetidas enquanto o pedido decorre', async () => {
      const { harness, pagina, raiz } = await abrir();
      pagina.formulario.setValue(validos);
      pagina.registar();
      pagina.registar();
      harness.detectChanges();
      const botao = raiz.querySelector<HTMLButtonElement>('button[type="submit"]');
      expect(botao?.disabled).toBeTrue();
      expect(botao?.textContent).toContain('A criar a conta');
      http.expectOne(urlRegisto).flush({ mensagem: 'ok' }, { status: 202, statusText: 'Accepted' });
    });
  });

  describe('validação no cliente', () => {
    async function recusa(alteracoes: Partial<typeof validos>, mensagem: string) {
      const { harness, pagina, raiz } = await abrir();
      pagina.formulario.setValue({ ...validos, ...alteracoes });
      pagina.registar();
      harness.detectChanges();
      http.expectNone(urlRegisto);
      expect(raiz.textContent).toContain(mensagem);
    }

    it('explica o que falta num formulário vazio e não envia', async () => {
      const { harness, pagina, raiz } = await abrir();
      pagina.registar();
      harness.detectChanges();
      http.expectNone(urlRegisto);
      for (const mensagem of [
        'Indica o teu nome.',
        'Indica o teu email académico.',
        'Indica uma palavra-passe.',
        'Repete a palavra-passe.',
      ])
        expect(raiz.textContent).toContain(mensagem);
      expect(raiz.querySelector('#nome')?.getAttribute('aria-invalid')).toBe('true');
    });
    it('recusa um nome só com espaços', () => recusa({ nome: '   ' }, 'Indica o teu nome.'));
    it('recusa um nome com mais de 100 caracteres', () =>
      recusa({ nome: 'a'.repeat(101) }, 'no máximo 100'));
    it('recusa um email mal formado', () =>
      recusa({ email: 'sem-arroba' }, 'Indica um email válido.'));
    it('recusa um email demasiado longo', () =>
      recusa({ email: `ana@${'a.'.repeat(130)}pt` }, 'Indica um email válido.'));
    it('recusa uma palavra-passe curta (P-08)', () =>
      recusa({ password: 'Abc1', confirmacao: 'Abc1' }, 'pelo menos 8 caracteres'));
    it('recusa uma palavra-passe sem dígitos (P-08)', () =>
      recusa({ password: 'sodeletras', confirmacao: 'sodeletras' }, 'uma letra e um dígito'));
    it('recusa confirmações diferentes', () =>
      recusa({ confirmacao: 'Outra1234' }, 'As palavras-passe não coincidem.'));
    it('revalida a confirmação quando a palavra-passe muda', async () => {
      const { pagina } = await abrir();
      pagina.formulario.setValue({ ...validos, confirmacao: 'Outra1234' });
      expect(pagina.formulario.controls.confirmacao.hasError('diferente')).toBeTrue();
      pagina.formulario.controls.password.setValue('Outra1234');
      expect(pagina.formulario.valid).toBeTrue();
    });
  });

  describe('erros da API', () => {
    for (const [status, mensagem] of [
      [400, 'É necessário um email académico permitido.'],
      [409, 'Email já registado.'],
    ] as const) {
      it(`${status} mantém o formulário e mostra a mensagem só junto dele`, async () => {
        const { harness, pagina, raiz } = await abrir();
        pagina.formulario.setValue(validos);
        pagina.registar();
        http.expectOne(urlRegisto).flush({ erro: mensagem }, { status, statusText: 'Erro' });
        harness.detectChanges();
        expect(raiz.querySelector('[role="alert"]')?.textContent).toContain(mensagem);
        expect(raiz.querySelector('form')).not.toBeNull();
        expect(pagina.aRegistar()).toBeFalse();
        expect(pagina.formulario.controls.email.value).toBe('ana@estg.ipp.pt');
        expect(TestBed.inject(NotificacaoUiService).mensagem()).toBeNull();
      });
    }
    it('503 da API avisa que a conta existe e esconde o formulário', async () => {
      const { harness, pagina, raiz } = await abrir();
      pagina.formulario.setValue(validos);
      pagina.registar();
      http
        .expectOne(urlRegisto)
        .flush(
          { erro: 'Conta criada, mas o envio do email falhou.' },
          { status: 503, statusText: 'Service Unavailable' },
        );
      harness.detectChanges();
      expect(raiz.querySelector('h1')?.textContent).toContain('Falta a confirmação');
      expect(raiz.querySelector('[role="alert"]')?.textContent).toContain('Conta criada');
      expect(raiz.querySelector('form')).toBeNull();
    });
    it('503 da infraestrutura não afirma que a conta foi criada', async () => {
      const { harness, pagina, raiz } = await abrir();
      pagina.formulario.setValue(validos);
      pagina.registar();
      http
        .expectOne(urlRegisto)
        .flush('<html>indisponível</html>', { status: 503, statusText: 'Service Unavailable' });
      harness.detectChanges();
      expect(raiz.querySelector('form')).not.toBeNull();
      expect(raiz.textContent).toContain('temporariamente indisponível');
      expect(raiz.textContent).not.toContain('CONTA CRIADA');
    });
    it('sem ligação ao servidor mantém o formulário e explica o problema', async () => {
      const { harness, pagina, raiz } = await abrir();
      pagina.formulario.setValue(validos);
      pagina.registar();
      http.expectOne(urlRegisto).error(new ProgressEvent('error'));
      harness.detectChanges();
      expect(raiz.querySelector('form')).not.toBeNull();
      expect(raiz.textContent).toContain('Não foi possível contactar o servidor');
    });
    for (const [status, resposta] of [
      [202, { mensagem: 'Verifica o email académico para confirmar a conta.' }],
      [503, { erro: 'Conta criada, mas o envio do email falhou.' }],
    ] as const) {
      it(`${status} oferece nova ligação com o email preenchido, sem o pôr no URL`, async () => {
        const { harness, pagina, raiz } = await abrir();
        pagina.formulario.setValue(validos);
        pagina.registar();
        http.expectOne(urlRegisto).flush(resposta, { status, statusText: 'Resposta' });
        harness.detectChanges();
        raiz.querySelector<HTMLAnchorElement>('a[href="/reenviar-confirmacao"]')?.click();
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(TestBed.inject(Router).url).toBe('/reenviar-confirmacao');
        expect(
          (harness.routeNativeElement as HTMLElement).querySelector<HTMLInputElement>('#email')
            ?.value,
        ).toBe('ana@estg.ipp.pt');
      });
    }
    it('409 oferece nova ligação junto do formulário (UC-F001-01 E4)', async () => {
      const { harness, pagina, raiz } = await abrir();
      pagina.formulario.setValue(validos);
      pagina.registar();
      http
        .expectOne(urlRegisto)
        .flush({ erro: 'Email já registado.' }, { status: 409, statusText: 'Conflict' });
      harness.detectChanges();
      expect(raiz.querySelector('a[href="/reenviar-confirmacao"]')).not.toBeNull();
      expect(raiz.querySelector('a[href="/entrar"]')).not.toBeNull();
    });
    it('limpa o erro anterior ao submeter de novo', async () => {
      const { harness, pagina, raiz } = await abrir();
      pagina.formulario.setValue(validos);
      pagina.registar();
      http
        .expectOne(urlRegisto)
        .flush({ erro: 'Email já registado.' }, { status: 409, statusText: 'Conflict' });
      pagina.registar();
      harness.detectChanges();
      expect(raiz.textContent).not.toContain('Email já registado.');
      http.expectOne(urlRegisto).flush({ mensagem: 'ok' }, { status: 202, statusText: 'Accepted' });
    });
  });

  describe('acesso ao registo (RF-F001-01)', () => {
    function iniciarSessao() {
      TestBed.inject(SessaoService).iniciar({
        accessToken: `a.${btoa(JSON.stringify({ sub: 'aluno-1', exp: Math.floor(Date.now() / 1000) + 3600 }))}.c`,
        expiresIn: 3600,
      });
    }

    it('a página inicial descreve o sistema e dá acesso ao registo a visitantes', async () => {
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl('/', InicioPage);
      const raiz = harness.routeNativeElement as HTMLElement;
      expect(raiz.textContent).toContain('Partilha viagens com outros estudantes');
      expect(raiz.querySelector('a[href="/registo"]')?.textContent).toContain('Criar conta');
    });
    it('a página de login liga ao registo', async () => {
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl('/entrar', LoginPage);
      expect(
        (harness.routeNativeElement as HTMLElement).querySelector('a[href="/registo"]'),
      ).not.toBeNull();
    });
    it('a página de login oferece nova ligação de confirmação (UC-F001-03)', async () => {
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl('/entrar', LoginPage);
      expect(
        (harness.routeNativeElement as HTMLElement).querySelector(
          'a[href="/reenviar-confirmacao"]',
        ),
      ).not.toBeNull();
    });
    it('quem tem sessão não vê o registo e volta ao início', async () => {
      iniciarSessao();
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl('/registo', InicioPage);
      expect(TestBed.inject(Router).url).toBe('/inicio');
      expect((harness.routeNativeElement as HTMLElement).querySelector('a[href="/registo"]')).toBe(
        null,
      );
    });
  });
});
