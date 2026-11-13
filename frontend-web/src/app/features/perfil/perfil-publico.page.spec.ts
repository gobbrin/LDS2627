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
import { PerfilPublico } from '../../shared/models/perfil';
import { PerfilPublicoPage } from './perfil-publico.page';

describe('I070 — perfil de outro utilizador (UC-F001-10)', () => {
  const base = 'http://localhost:5100/api/v1';
  const urlPublico = (id: string) => `${base}/utilizadores/${id}`;
  const publico = (alteracoes: Partial<PerfilPublico> = {}): PerfilPublico => ({
    id: 'outro-1',
    nome: 'Bruno Costa',
    curso: 'Engenharia Civil',
    instituicao: 'ISEP',
    ligacoesExternas: [{ id: 'l1', url: 'https://exemplo.pt/bruno', descricao: 'Portefólio' }],
    classificacaoMedia: 4.5,
    totalAvaliacoes: 2,
    ...alteracoes,
  });
  let http: HttpTestingController;
  let ui: NotificacaoUiService;

  async function abrir(id = 'outro-1') {
    const harness = await RouterTestingHarness.create();
    const pagina = await harness.navigateByUrl(`/utilizadores/${id}`, PerfilPublicoPage);
    harness.detectChanges();
    return { harness, pagina, raiz: harness.routeNativeElement as HTMLElement };
  }
  async function abrirCom(resposta: object) {
    const aberto = await abrir();
    http.expectOne(urlPublico('outro-1')).flush(resposta);
    aberto.harness.detectChanges();
    return aberto;
  }
  const botao = (raiz: HTMLElement, texto: string) =>
    [...raiz.querySelectorAll('button')].find((b) => b.textContent?.includes(texto));

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
    ui = TestBed.inject(NotificacaoUiService);
    TestBed.inject(SessaoService).iniciar({
      accessToken: `a.${btoa(JSON.stringify({ sub: 'aluno-1', exp: Math.floor(Date.now() / 1000) + 3600 }))}.c`,
      expiresIn: 3600,
    });
  });
  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
    sessionStorage.clear();
  });

  it('apresenta os campos públicos de P-10 (RF-F001-17, RF-F001-19)', async () => {
    const { raiz } = await abrirCom(publico());
    for (const valor of ['Bruno Costa', 'Engenharia Civil', 'ISEP', 'Portefólio', '4,5 / 5'])
      expect(raiz.textContent).toContain(valor);
  });

  it('não apresenta o email, mesmo que a resposta o trouxesse (P-10)', async () => {
    const { raiz } = await abrirCom({ ...publico(), email: 'bruno@isep.ipp.pt' });
    expect(raiz.textContent).not.toContain('bruno@isep.ipp.pt');
    expect(raiz.textContent).not.toContain('Email');
  });

  it('é só de leitura: não oferece edição', async () => {
    const { raiz } = await abrirCom(publico());
    expect(botao(raiz, 'Editar')).toBeUndefined();
    expect(raiz.querySelector('input, form')).toBeNull();
  });

  it('abre as ligações noutra janela, sem acesso à origem', async () => {
    const { raiz } = await abrirCom(publico());
    const ligacao = raiz.querySelector<HTMLAnchorElement>('.ligacoes a');
    expect(ligacao?.href).toBe('https://exemplo.pt/bruno');
    expect(ligacao?.target).toBe('_blank');
    expect(ligacao?.rel).toBe('noopener noreferrer');
  });

  it('indica os dados em falta e «sem avaliações» (RF-F007-10)', async () => {
    const { raiz } = await abrirCom(
      publico({
        curso: null,
        instituicao: null,
        ligacoesExternas: [],
        classificacaoMedia: null,
        totalAvaliacoes: 0,
      }),
    );
    for (const texto of ['Não indicado', 'Não indicada', 'Sem ligações externas', 'Sem avaliações'])
      expect(raiz.textContent).toContain(texto);
  });

  it('mostra o estado de carregamento até chegar a resposta', async () => {
    const { harness, raiz } = await abrir();
    expect(raiz.querySelector('[role="status"]')?.textContent).toContain('A carregar');
    http.expectOne(urlPublico('outro-1')).flush(publico());
    harness.detectChanges();
    expect(raiz.querySelector('[role="status"]')).toBeNull();
  });

  it('informa que o perfil não foi encontrado, sem oferecer repetição (E1)', async () => {
    const { harness, raiz } = await abrir();
    http
      .expectOne(urlPublico('outro-1'))
      .flush({ erro: 'Utilizador não encontrado.' }, { status: 404, statusText: 'Not Found' });
    harness.detectChanges();
    expect(raiz.querySelector('[role="alert"]')?.textContent).toContain('Perfil não encontrado.');
    expect(botao(raiz, 'Tentar novamente')).toBeUndefined();
    expect(raiz.querySelector('a[href="/inicio"]')).not.toBeNull();
    expect(ui.mensagem()).toBeNull();
  });

  it('num 404 sem corpo, como o de um identificador mal formado, diz o mesmo', async () => {
    const { harness, raiz } = await abrir('nao-e-guid');
    http.expectOne(urlPublico('nao-e-guid')).flush(null, { status: 404, statusText: 'Not Found' });
    harness.detectChanges();
    expect(raiz.querySelector('[role="alert"]')?.textContent).toContain('Perfil não encontrado.');
  });

  it('noutro erro mostra-o só na página e permite repetir', async () => {
    const { harness, raiz } = await abrir();
    http
      .expectOne(urlPublico('outro-1'))
      .error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown' });
    harness.detectChanges();
    expect(raiz.querySelector('[role="alert"]')?.textContent).toContain('contactar o servidor');
    expect(ui.mensagem()).toBeNull();
    botao(raiz, 'Tentar novamente')?.click();
    harness.detectChanges();
    expect(raiz.querySelector('[role="status"]')).not.toBeNull();
    http.expectOne(urlPublico('outro-1')).flush(publico());
    harness.detectChanges();
    expect(raiz.textContent).toContain('Bruno Costa');
    expect(raiz.querySelector('[role="alert"]')).toBeNull();
  });

  it('ao mudar de pessoa na mesma página, ignora a resposta anterior', async () => {
    const { harness, raiz } = await abrir();
    const anterior = http.expectOne(urlPublico('outro-1'));
    await harness.navigateByUrl('/utilizadores/outro-2');
    harness.detectChanges();
    expect(anterior.cancelled).toBeTrue();
    http.expectOne(urlPublico('outro-2')).flush(publico({ id: 'outro-2', nome: 'Carla Dias' }));
    harness.detectChanges();
    expect(raiz.textContent).toContain('Carla Dias');
    expect(raiz.textContent).not.toContain('Bruno Costa');
  });

  it('sem sessão manda para a entrada e guarda o destino', async () => {
    TestBed.inject(SessaoService).terminar();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/utilizadores/outro-1');
    expect(TestBed.inject(Router).url).toBe('/entrar?regresso=%2Futilizadores%2Foutro-1');
  });
});
