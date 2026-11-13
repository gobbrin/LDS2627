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
import { ReenvioConfirmacaoPage } from './reenvio-confirmacao.page';

describe('I070 — nova ligação de confirmação', () => {
  const base = 'http://localhost:5100/api/v1';
  const urlReenvio = base + '/autenticacao/reenviar-confirmacao';
  const NEUTRA =
    'Se existir uma conta pendente associada a este email, receberás uma nova ligação.';
  const ACEITE = { status: 202, statusText: 'Accepted' };
  let http: HttpTestingController;

  async function abrir() {
    const harness = await RouterTestingHarness.create();
    const pagina = await harness.navigateByUrl('/reenviar-confirmacao', ReenvioConfirmacaoPage);
    return { harness, pagina, raiz: harness.routeNativeElement as HTMLElement };
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

  it('202 mostra a resposta neutra da API, esconde o formulário e liga à entrada (RF-F001-21)', async () => {
    const { harness, pagina, raiz } = await abrir();
    pagina.formulario.setValue({ email: 'ana@estg.ipp.pt' });
    pagina.pedir();
    const pedido = http.expectOne(urlReenvio);
    expect(pedido.request.method).toBe('POST');
    expect(pedido.request.body).toEqual({ email: 'ana@estg.ipp.pt' });
    pedido.flush({ mensagem: NEUTRA }, ACEITE);
    harness.detectChanges();
    expect(raiz.querySelector('[role="status"]')?.textContent).toContain(NEUTRA);
    expect(raiz.querySelector('form')).toBeNull();
    expect(raiz.querySelector('a[href="/entrar"]')).not.toBeNull();
  });

  it('não permite pedidos repetidos enquanto o primeiro decorre', async () => {
    const { harness, pagina, raiz } = await abrir();
    pagina.formulario.setValue({ email: 'ana@estg.ipp.pt' });
    pagina.pedir();
    pagina.pedir();
    harness.detectChanges();
    const botao = raiz.querySelector<HTMLButtonElement>('button[type="submit"]');
    expect(botao?.disabled).toBeTrue();
    expect(botao?.textContent).toContain('A enviar');
    http.expectOne(urlReenvio).flush({ mensagem: NEUTRA }, ACEITE);
  });

  describe('email indicado', () => {
    for (const [valor, mensagem] of [
      ['', 'Indica o teu email académico.'],
      ['sem-arroba', 'Indica um email válido.'],
    ] as const) {
      it(`recusa «${valor}» sem chamar a API (UC-F001-03 E1)`, async () => {
        const { harness, pagina, raiz } = await abrir();
        pagina.formulario.setValue({ email: valor });
        pagina.pedir();
        harness.detectChanges();
        http.expectNone(urlReenvio);
        expect(raiz.textContent).toContain(mensagem);
        expect(raiz.querySelector('#email')?.getAttribute('aria-invalid')).toBe('true');
      });
    }

    it('vem preenchido quando já é conhecido, sem aparecer no URL', async () => {
      const harness = await RouterTestingHarness.create();
      const router = TestBed.inject(Router);
      await router.navigateByUrl('/reenviar-confirmacao', { state: { email: 'ana@estg.ipp.pt' } });
      harness.detectChanges();
      const entrada = (harness.routeNativeElement as HTMLElement).querySelector('input');
      expect(entrada?.value).toBe('ana@estg.ipp.pt');
      expect(router.url).toBe('/reenviar-confirmacao');
    });

    it('fica vazio se o estado da navegação não trouxer um texto', async () => {
      const harness = await RouterTestingHarness.create();
      await TestBed.inject(Router).navigateByUrl('/reenviar-confirmacao', {
        state: { email: 42 },
      });
      harness.detectChanges();
      expect((harness.routeNativeElement as HTMLElement).querySelector('input')?.value).toBe('');
    });
  });

  describe('erros da API', () => {
    for (const [status, corpo, texto] of [
      [400, { erro: 'Email inválido.' }, 'Email inválido.'],
      [429, null, 'demasiados pedidos'],
    ] as const) {
      it(`${status} mantém o formulário e mostra o erro só junto dele`, async () => {
        const { harness, pagina, raiz } = await abrir();
        pagina.formulario.setValue({ email: 'ana@estg.ipp.pt' });
        pagina.pedir();
        http.expectOne(urlReenvio).flush(corpo, { status, statusText: 'Erro' });
        harness.detectChanges();
        expect(raiz.querySelector('[role="alert"]')?.textContent).toContain(texto);
        expect(raiz.querySelector('form')).not.toBeNull();
        expect(pagina.aEnviar()).toBeFalse();
        expect(TestBed.inject(NotificacaoUiService).mensagem()).toBeNull();
      });
    }

    it('limpa o erro anterior ao pedir de novo', async () => {
      const { harness, pagina, raiz } = await abrir();
      pagina.formulario.setValue({ email: 'ana@estg.ipp.pt' });
      pagina.pedir();
      http.expectOne(urlReenvio).error(new ProgressEvent('error'));
      harness.detectChanges();
      expect(raiz.textContent).toContain('Não foi possível contactar o servidor');
      pagina.pedir();
      harness.detectChanges();
      expect(raiz.textContent).not.toContain('Não foi possível contactar o servidor');
      http.expectOne(urlReenvio).flush({ mensagem: NEUTRA }, ACEITE);
    });
  });

  it('quem tem sessão não vê esta página e volta ao início', async () => {
    TestBed.inject(SessaoService).iniciar({
      accessToken: `a.${btoa(JSON.stringify({ sub: 'aluno-1', exp: Math.floor(Date.now() / 1000) + 3600 }))}.c`,
      expiresIn: 3600,
    });
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/reenviar-confirmacao', InicioPage);
    expect(TestBed.inject(Router).url).toBe('/inicio');
  });
});
