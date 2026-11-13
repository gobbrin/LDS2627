import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';
import { CONFIGURACAO } from '../../core/configuracao';
import { autenticacaoInterceptor, erroInterceptor } from '../../core/interceptors';
import { NotificacaoUiService } from '../../core/notificacao-ui.service';
import { SessaoService } from '../../core/sessao.service';
import { LigacaoPerfil, PerfilProprio } from '../../shared/models/perfil';
import { LigacoesPerfilComponent } from './ligacoes-perfil.component';
import { PerfilPage } from './perfil.page';
import { By } from '@angular/platform-browser';

describe('I070 — perfil próprio', () => {
  const base = 'http://localhost:5100/api/v1';
  const urlPerfil = base + '/utilizadores/me';
  const urlLigacoes = urlPerfil + '/ligacoes';
  const ligacao = (n: number, descricao: string | null = null): LigacaoPerfil => ({
    id: `l${n}`,
    url: `https://exemplo.pt/${n}`,
    descricao,
  });
  const perfil = (alteracoes: Partial<PerfilProprio> = {}): PerfilProprio => ({
    id: 'aluno-1',
    nome: 'Ana Silva',
    email: 'ana@estg.ipp.pt',
    emailConfirmado: true,
    curso: 'Informática',
    instituicao: 'ESTG',
    criadoEm: '2026-11-01T10:00:00Z',
    ligacoesExternas: [],
    classificacaoMedia: null,
    totalAvaliacoes: 0,
    ...alteracoes,
  });
  const cinco = [1, 2, 3, 4, 5].map((n) => ligacao(n));
  let http: HttpTestingController;
  let ui: NotificacaoUiService;

  async function abrir(dados = perfil()) {
    const harness = await RouterTestingHarness.create();
    const pagina = await harness.navigateByUrl('/perfil', PerfilPage);
    http.expectOne(urlPerfil).flush(dados);
    harness.detectChanges();
    const raiz = harness.routeNativeElement as HTMLElement;
    return { harness, pagina, raiz };
  }
  async function abrirEdicao(dados = perfil()) {
    const aberto = await abrir(dados);
    aberto.raiz.querySelector<HTMLButtonElement>('#editar-perfil')?.click();
    aberto.harness.detectChanges();
    await aberto.harness.fixture.whenStable();
    const ligacoes = aberto.harness.fixture.debugElement.query(
      By.directive(LigacoesPerfilComponent),
    ).componentInstance as LigacoesPerfilComponent;
    return { ...aberto, ligacoes };
  }
  const recusar = (url: string, status: number, erro: string) =>
    http.expectOne(url).flush({ erro }, { status, statusText: 'Erro' });
  const botao = (raiz: HTMLElement, texto: string) =>
    [...raiz.querySelectorAll('button')].find((b) => b.textContent?.includes(texto));
  const textos = (raiz: HTMLElement) => raiz.textContent ?? '';

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

  describe('consulta (UC-F001-06)', () => {
    it('apresenta o nome, o email, o curso e a instituição (RF-F001-20)', async () => {
      const { raiz } = await abrir();
      for (const valor of ['Ana Silva', 'ana@estg.ipp.pt', 'Informática', 'ESTG'])
        expect(textos(raiz)).toContain(valor);
    });

    it('apresenta as ligações como ligações, que abrem noutra janela sem acesso à origem', async () => {
      const { raiz } = await abrir(
        perfil({ ligacoesExternas: [ligacao(1, 'LinkedIn'), ligacao(2)] }),
      );
      const ligacoes = raiz.querySelectorAll<HTMLAnchorElement>('.ligacoes a');
      expect(ligacoes.length).toBe(2);
      expect(ligacoes[0].textContent).toContain('LinkedIn');
      expect(ligacoes[0].href).toBe('https://exemplo.pt/1');
      expect(ligacoes[0].target).toBe('_blank');
      expect(ligacoes[0].rel).toBe('noopener noreferrer');
      expect(ligacoes[1].textContent).toContain('https://exemplo.pt/2');
    });

    it('indica quando faltam dados opcionais e avaliações (RF-F007-10)', async () => {
      const { raiz } = await abrir(perfil({ curso: null, instituicao: null }));
      for (const texto of [
        'Não indicado',
        'Não indicada',
        'Sem ligações externas',
        'Sem avaliações',
      ])
        expect(textos(raiz)).toContain(texto);
    });

    for (const [media, texto] of [
      [4.5, '4,5 / 5'],
      [5, '5,0 / 5'],
      [3.67, '3,7 / 5'],
    ] as const) {
      it(`apresenta a média ${media} como ${texto}`, async () => {
        const { raiz } = await abrir(perfil({ classificacaoMedia: media, totalAvaliacoes: 2 }));
        expect(textos(raiz)).toContain(texto);
      });
    }

    it('mostra o erro da API só na página e permite repetir', async () => {
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl('/perfil', PerfilPage);
      recusar(urlPerfil, 404, 'Utilizador não encontrado.');
      harness.detectChanges();
      const raiz = harness.routeNativeElement as HTMLElement;
      expect(raiz.querySelector('[role="alert"]')?.textContent).toContain(
        'Utilizador não encontrado.',
      );
      expect(ui.mensagem()).toBeNull();
      botao(raiz, 'Tentar novamente')?.click();
      http.expectOne(urlPerfil).flush(perfil());
      harness.detectChanges();
      expect(textos(raiz)).toContain('Ana Silva');
      expect(raiz.querySelector('[role="alert"]')).toBeNull();
    });
  });

  describe('alteração dos dados (UC-F001-07)', () => {
    it('apresenta os valores atuais e dá o foco ao primeiro campo', async () => {
      const { pagina, raiz } = await abrirEdicao();
      expect(pagina.formulario.getRawValue()).toEqual({
        nome: 'Ana Silva',
        curso: 'Informática',
        instituicao: 'ESTG',
      });
      expect(document.activeElement?.id).toBe('nome');
      expect(raiz.querySelector('input[type="email"], #email')).toBeNull();
    });

    it('preenche com vazio o curso e a instituição por indicar', async () => {
      const { pagina } = await abrirEdicao(perfil({ curso: null, instituicao: null }));
      expect(pagina.formulario.getRawValue().curso).toBe('');
      expect(pagina.formulario.getRawValue().instituicao).toBe('');
    });

    it('guarda sem espaços a mais, com os vazios a null (RF-F001-14)', async () => {
      const { pagina } = await abrirEdicao();
      pagina.formulario.setValue({ nome: '  Ana Costa ', curso: '   ', instituicao: ' IPP ' });
      pagina.guardar();
      const pedido = http.expectOne(urlPerfil);
      expect(pedido.request.method).toBe('PUT');
      expect(pedido.request.body).toEqual({ nome: 'Ana Costa', curso: null, instituicao: 'IPP' });
      pedido.flush({ ...perfil(), nome: 'Ana Costa', curso: null, instituicao: 'IPP' });
    });

    it('volta ao perfil atualizado, mantém as ligações e confirma', async () => {
      const { harness, pagina, raiz } = await abrirEdicao(
        perfil({
          ligacoesExternas: [ligacao(1, 'GitHub')],
          classificacaoMedia: 4,
          totalAvaliacoes: 1,
        }),
      );
      pagina.formulario.controls.nome.setValue('Ana Costa');
      pagina.guardar();
      const resposta: Partial<PerfilProprio> = { ...perfil(), nome: 'Ana Costa' };
      delete resposta.ligacoesExternas;
      delete resposta.classificacaoMedia;
      delete resposta.totalAvaliacoes;
      delete resposta.criadoEm;
      http.expectOne(urlPerfil).flush(resposta);
      harness.detectChanges();
      await harness.fixture.whenStable();
      expect(textos(raiz)).toContain('Ana Costa');
      expect(textos(raiz)).toContain('GitHub');
      expect(textos(raiz)).toContain('4,0 / 5');
      expect(raiz.querySelector('form')).toBeNull();
      expect(ui.mensagem()).toEqual({ tipo: 'sucesso', texto: 'Perfil atualizado.' });
      expect(document.activeElement?.id).toBe('editar-perfil');
    });

    it('ao cancelar volta ao perfil sem pedidos nem alterações (A1)', async () => {
      const { harness, pagina, raiz } = await abrirEdicao();
      pagina.formulario.controls.nome.setValue('Outro Nome');
      botao(raiz, 'Cancelar')?.click();
      harness.detectChanges();
      await harness.fixture.whenStable();
      http.expectNone(urlPerfil);
      expect(textos(raiz)).toContain('Ana Silva');
      expect(textos(raiz)).not.toContain('Outro Nome');
      expect(document.activeElement?.id).toBe('editar-perfil');
    });

    it('ao reabrir descarta o que foi cancelado', async () => {
      const { harness, pagina, raiz } = await abrirEdicao();
      pagina.formulario.controls.nome.setValue('');
      pagina.guardar();
      harness.detectChanges();
      expect(textos(raiz)).toContain('Indica o teu nome.');
      botao(raiz, 'Cancelar')?.click();
      harness.detectChanges();
      botao(raiz, 'Editar perfil')?.click();
      harness.detectChanges();
      expect(pagina.formulario.controls.nome.value).toBe('Ana Silva');
      expect(textos(raiz)).not.toContain('Indica o teu nome.');
    });

    for (const [campo, valor, mensagem] of [
      ['nome', '', 'Indica o teu nome.'],
      ['nome', '   ', 'Indica o teu nome.'],
      ['nome', 'a'.repeat(101), 'O nome tem no máximo 100 caracteres.'],
      ['curso', 'a'.repeat(151), 'O curso tem no máximo 150 caracteres.'],
      ['instituicao', 'a'.repeat(151), 'A instituição tem no máximo 150 caracteres.'],
    ] as const) {
      it(`recusa ${campo} com ${valor.length} caracteres e indica o campo (E1)`, async () => {
        const { harness, pagina, raiz } = await abrirEdicao();
        pagina.formulario.controls[campo].setValue(valor);
        pagina.guardar();
        harness.detectChanges();
        http.expectNone(urlPerfil);
        expect(textos(raiz)).toContain(mensagem);
        expect(raiz.querySelector(`#${campo}`)?.getAttribute('aria-invalid')).toBe('true');
      });
    }

    it('aceita os tamanhos máximos de P-26', async () => {
      const { pagina } = await abrirEdicao();
      const dados = {
        nome: 'a'.repeat(100),
        curso: 'b'.repeat(150),
        instituicao: 'c'.repeat(150),
      };
      pagina.formulario.setValue(dados);
      pagina.guardar();
      const pedido = http.expectOne(urlPerfil);
      expect(pedido.request.body).toEqual(dados);
      pedido.flush({ ...perfil(), ...dados });
    });

    it('mostra a recusa da API junto do formulário e não altera o perfil', async () => {
      const { harness, pagina, raiz } = await abrirEdicao();
      pagina.formulario.controls.nome.setValue('Ana Costa');
      pagina.guardar();
      recusar(urlPerfil, 400, 'Dados de perfil inválidos.');
      harness.detectChanges();
      expect(raiz.querySelector('form [role="alert"]')?.textContent).toContain(
        'Dados de perfil inválidos.',
      );
      expect(ui.mensagem()).toBeNull();
      expect(pagina.aEditar()).toBeTrue();
      expect(pagina.perfil()?.nome).toBe('Ana Silva');
      expect(pagina.formulario.controls.nome.value).toBe('Ana Costa');
    });

    it('limpa o erro anterior ao tentar de novo', async () => {
      const { harness, pagina, raiz } = await abrirEdicao();
      pagina.guardar();
      http.expectOne(urlPerfil).error(new ProgressEvent('error'));
      harness.detectChanges();
      expect(raiz.querySelector('form [role="alert"]')?.textContent).toContain(
        'Não foi possível contactar o servidor',
      );
      pagina.guardar();
      harness.detectChanges();
      expect(raiz.querySelector('form [role="alert"]')).toBeNull();
      http.expectOne(urlPerfil).flush(perfil());
    });

    it('não permite pedidos repetidos nem cancelar enquanto guarda', async () => {
      const { harness, pagina, raiz } = await abrirEdicao();
      pagina.guardar();
      pagina.guardar();
      harness.detectChanges();
      expect(raiz.querySelector<HTMLButtonElement>('button[type="submit"]')?.disabled).toBeTrue();
      expect(botao(raiz, 'A guardar')).toBeDefined();
      expect(botao(raiz, 'Cancelar')?.disabled).toBeTrue();
      expect(botao(raiz, 'Voltar ao perfil')?.disabled).toBeTrue();
      http.expectOne(urlPerfil).flush(perfil());
    });
  });

  describe('associar ligações (UC-F001-08)', () => {
    const adicionar = (l: LigacoesPerfilComponent, url: string, descricao = '') => {
      l.formulario.setValue({ url, descricao });
      l.adicionar();
    };

    it('envia o endereço e a descrição sem espaços a mais (RF-F001-15)', async () => {
      const { harness, raiz, ligacoes } = await abrirEdicao();
      adicionar(ligacoes, ' https://www.linkedin.com/in/ana ', ' LinkedIn ');
      const pedido = http.expectOne(urlLigacoes);
      expect(pedido.request.method).toBe('POST');
      expect(pedido.request.body).toEqual({
        url: 'https://www.linkedin.com/in/ana',
        descricao: 'LinkedIn',
      });
      pedido.flush({ id: 'n', url: 'https://www.linkedin.com/in/ana', descricao: 'LinkedIn' });
      harness.detectChanges();
      expect(raiz.querySelector('.gestao-ligacoes a')?.textContent).toContain('LinkedIn');
      expect(raiz.querySelector('.gestao-ligacoes small')?.textContent).toContain('linkedin.com');
      expect(ligacoes.formulario.getRawValue()).toEqual({ url: '', descricao: '' });
      expect(ui.mensagem()).toEqual({ tipo: 'sucesso', texto: 'Ligação adicionada.' });
    });

    it('envia a descrição vazia como null', async () => {
      const { ligacoes } = await abrirEdicao();
      adicionar(ligacoes, 'https://exemplo.pt');
      const pedido = http.expectOne(urlLigacoes);
      expect(pedido.request.body.descricao).toBeNull();
      pedido.flush(ligacao(1));
    });

    it('mantém os dados por guardar do perfil ao acrescentar uma ligação', async () => {
      const { harness, pagina, ligacoes } = await abrirEdicao();
      pagina.formulario.controls.nome.setValue('Por guardar');
      adicionar(ligacoes, 'https://exemplo.pt');
      http.expectOne(urlLigacoes).flush(ligacao(1));
      harness.detectChanges();
      expect(pagina.aEditar()).toBeTrue();
      expect(pagina.formulario.controls.nome.value).toBe('Por guardar');
    });

    for (const [url, mensagem] of [
      ['', 'Indica o endereço da ligação.'],
      ['javascript:alert(1)', 'a começar por http:// ou https://'],
      ['exemplo.pt', 'a começar por http:// ou https://'],
      [`https://exemplo.pt/${'a'.repeat(2048)}`, 'no máximo 2048 caracteres'],
    ] as const) {
      it(`recusa «${url.slice(0, 30)}» e indica o formato aceite (E1)`, async () => {
        const { harness, raiz, ligacoes } = await abrirEdicao();
        adicionar(ligacoes, url);
        harness.detectChanges();
        http.expectNone(urlLigacoes);
        expect(textos(raiz)).toContain(mensagem);
        expect(raiz.querySelector('#url')?.getAttribute('aria-invalid')).toBe('true');
      });
    }

    it('recusa uma descrição com mais de 100 caracteres', async () => {
      const { harness, raiz, ligacoes } = await abrirEdicao();
      adicionar(ligacoes, 'https://exemplo.pt', 'a'.repeat(101));
      harness.detectChanges();
      http.expectNone(urlLigacoes);
      expect(textos(raiz)).toContain('A descrição tem no máximo 100 caracteres.');
    });

    it('aceita os tamanhos máximos', async () => {
      const { ligacoes } = await abrirEdicao();
      const url = `https://exemplo.pt/${'a'.repeat(2048 - 19)}`;
      adicionar(ligacoes, url, 'd'.repeat(100));
      http.expectOne(urlLigacoes).flush({ id: 'n', url, descricao: 'd'.repeat(100) });
    });

    it('no máximo de P-27 esconde o formulário, informa e sugere remover (E2)', async () => {
      const { raiz } = await abrirEdicao(perfil({ ligacoesExternas: cinco }));
      expect(raiz.querySelector('#url')).toBeNull();
      expect(textos(raiz)).toContain('Atingiste o máximo de 5 ligações');
      expect(textos(raiz)).toContain('Remove uma');
    });

    it('ao chegar ao máximo passa o foco para a lista', async () => {
      const { harness, raiz, ligacoes } = await abrirEdicao(
        perfil({ ligacoesExternas: cinco.slice(0, 4) }),
      );
      adicionar(ligacoes, 'https://exemplo.pt/5');
      http.expectOne(urlLigacoes).flush(ligacao(5));
      harness.detectChanges();
      await harness.fixture.whenStable();
      expect(raiz.querySelector('#url')).toBeNull();
      expect(document.activeElement?.id).toBe('titulo-ligacoes');
    });

    it('com o máximo atingido noutro dispositivo, informa e atualiza a lista', async () => {
      const { harness, raiz, ligacoes } = await abrirEdicao(
        perfil({ ligacoesExternas: cinco.slice(0, 4) }),
      );
      adicionar(ligacoes, 'https://exemplo.pt/6');
      recusar(urlLigacoes, 400, 'O perfil já tem o número máximo de ligações.');
      harness.detectChanges();
      expect(raiz.querySelector('form:last-of-type [role="alert"]')?.textContent).toContain(
        'número máximo',
      );
      http.expectOne(urlPerfil).flush(perfil({ ligacoesExternas: cinco }));
      harness.detectChanges();
      expect(raiz.querySelectorAll('.gestao-ligacoes li').length).toBe(5);
      expect(textos(raiz)).toContain('Atingiste o máximo de 5 ligações');
    });

    it('sem ligação mostra o erro, mantém o texto e não recarrega', async () => {
      const { harness, raiz, ligacoes } = await abrirEdicao();
      adicionar(ligacoes, 'https://exemplo.pt');
      http.expectOne(urlLigacoes).error(new ProgressEvent('error'));
      harness.detectChanges();
      http.expectNone(urlPerfil);
      expect(textos(raiz)).toContain('Não foi possível contactar o servidor');
      expect(ligacoes.formulario.controls.url.value).toBe('https://exemplo.pt');
      expect(ui.mensagem()).toBeNull();
    });

    it('não permite pedidos repetidos enquanto acrescenta', async () => {
      const { harness, raiz, ligacoes } = await abrirEdicao();
      adicionar(ligacoes, 'https://exemplo.pt');
      ligacoes.adicionar();
      harness.detectChanges();
      expect(botao(raiz, 'A adicionar')?.disabled).toBeTrue();
      http.expectOne(urlLigacoes).flush(ligacao(1));
    });

    it('mantém a recusa à vista se a atualização seguinte falhar', async () => {
      const { harness, raiz, ligacoes } = await abrirEdicao();
      adicionar(ligacoes, 'https://exemplo.pt');
      recusar(urlLigacoes, 400, 'Ligação inválida: indica um URL http ou https.');
      http.expectOne(urlPerfil).error(new ProgressEvent('error'));
      harness.detectChanges();
      expect(textos(raiz)).toContain('Ligação inválida');
      expect(ui.mensagem()).toBeNull();
    });
  });

  describe('remover ligações (UC-F001-09)', () => {
    const urlRemover = urlLigacoes + '/l1';
    const comDuas = () => perfil({ ligacoesExternas: [ligacao(1, 'GitHub'), ligacao(2)] });
    async function aConfirmar(dados = comDuas()) {
      const aberto = await abrirEdicao(dados);
      aberto.raiz.querySelector<HTMLButtonElement>('#remover-l1')?.click();
      aberto.harness.detectChanges();
      await aberto.harness.fixture.whenStable();
      return aberto;
    }
    const sim = (raiz: HTMLElement) => botao(raiz, 'Sim, remover')?.click();

    it('pede confirmação antes de remover, com o foco na opção segura (passo 2)', async () => {
      const { raiz } = await aConfirmar();
      http.expectNone(urlRemover);
      expect(textos(raiz)).toContain('Remover esta ligação?');
      expect(document.activeElement?.id).toBe('manter-l1');
    });

    it('identifica cada botão de remover para leitores de ecrã', async () => {
      const { raiz } = await abrirEdicao(comDuas());
      expect(raiz.querySelector('#remover-l1')?.getAttribute('aria-label')).toBe('Remover GitHub');
      expect(raiz.querySelector('#remover-l2')?.getAttribute('aria-label')).toBe(
        'Remover https://exemplo.pt/2',
      );
    });

    it('sem confirmação fica tudo como estava (A1)', async () => {
      const { harness, raiz } = await aConfirmar();
      botao(raiz, 'Não')?.click();
      harness.detectChanges();
      await harness.fixture.whenStable();
      http.expectNone(urlRemover);
      expect(raiz.querySelectorAll('.gestao-ligacoes li').length).toBe(2);
      expect(document.activeElement?.id).toBe('remover-l1');
    });

    it('remove a ligação e apresenta a lista atualizada (RF-F001-22)', async () => {
      const { harness, raiz } = await aConfirmar();
      sim(raiz);
      const pedido = http.expectOne(urlRemover);
      expect(pedido.request.method).toBe('DELETE');
      pedido.flush(null, { status: 204, statusText: 'No Content' });
      harness.detectChanges();
      await harness.fixture.whenStable();
      expect(textos(raiz)).not.toContain('GitHub');
      expect(raiz.querySelectorAll('.gestao-ligacoes li').length).toBe(1);
      expect(ui.mensagem()).toEqual({ tipo: 'sucesso', texto: 'Ligação removida.' });
      expect(document.activeElement?.id).toBe('titulo-ligacoes');
    });

    it('ao voltar ao perfil retira a notificação da edição', async () => {
      const { harness, raiz } = await aConfirmar();
      sim(raiz);
      http.expectOne(urlRemover).flush(null, { status: 204, statusText: 'No Content' });
      harness.detectChanges();
      expect(ui.mensagem()?.texto).toBe('Ligação removida.');
      botao(raiz, 'Voltar ao perfil')?.click();
      harness.detectChanges();
      expect(ui.mensagem()).toBeNull();
    });

    it('a remoção aparece no perfil ao voltar', async () => {
      const { harness, raiz } = await aConfirmar();
      sim(raiz);
      http.expectOne(urlRemover).flush(null, { status: 204, statusText: 'No Content' });
      harness.detectChanges();
      botao(raiz, 'Voltar ao perfil')?.click();
      harness.detectChanges();
      expect(textos(raiz)).not.toContain('GitHub');
      expect(raiz.querySelectorAll('.ligacoes a').length).toBe(1);
    });

    it('remover no máximo volta a mostrar o formulário', async () => {
      const { harness, raiz } = await aConfirmar(perfil({ ligacoesExternas: cinco }));
      sim(raiz);
      http.expectOne(urlRemover).flush(null, { status: 204, statusText: 'No Content' });
      harness.detectChanges();
      expect(raiz.querySelector('#url')).not.toBeNull();
      expect(textos(raiz)).not.toContain('Atingiste o máximo');
    });

    it('não repete o aviso de máximo ao reaparecer o formulário', async () => {
      const { harness, raiz, ligacoes } = await abrirEdicao(
        perfil({ ligacoesExternas: cinco.slice(0, 4) }),
      );
      ligacoes.formulario.setValue({ url: 'https://exemplo.pt/6', descricao: '' });
      ligacoes.adicionar();
      recusar(urlLigacoes, 400, 'O perfil já tem o número máximo de ligações.');
      http.expectOne(urlPerfil).flush(perfil({ ligacoesExternas: cinco }));
      harness.detectChanges();
      raiz.querySelector<HTMLButtonElement>('#remover-l1')?.click();
      harness.detectChanges();
      sim(raiz);
      http.expectOne(urlRemover).flush(null, { status: 204, statusText: 'No Content' });
      harness.detectChanges();
      expect(textos(raiz)).not.toContain('número máximo');
    });

    it('já removida noutro dispositivo: informa e apresenta o perfil atual (E1)', async () => {
      const { harness, raiz } = await aConfirmar();
      sim(raiz);
      recusar(urlRemover, 404, 'Ligação não encontrada.');
      harness.detectChanges();
      expect(raiz.querySelector('.gestao-ligacoes > [role="alert"]')?.textContent).toContain(
        'Ligação não encontrada.',
      );
      expect(textos(raiz)).not.toContain('GitHub');
      http.expectOne(urlPerfil).flush(perfil({ ligacoesExternas: [ligacao(2), ligacao(3)] }));
      harness.detectChanges();
      expect(raiz.querySelectorAll('.gestao-ligacoes li').length).toBe(2);
      expect(raiz.querySelector('#remover-l3')).not.toBeNull();
      expect(ui.mensagem()).toBeNull();
    });

    it('noutra falha mantém a ligação e permite repetir', async () => {
      const { harness, raiz } = await aConfirmar();
      sim(raiz);
      http.expectOne(urlRemover).error(new ProgressEvent('error'));
      harness.detectChanges();
      http.expectNone(urlPerfil);
      expect(textos(raiz)).toContain('Não foi possível contactar o servidor');
      expect(raiz.querySelectorAll('.gestao-ligacoes li').length).toBe(2);
      sim(raiz);
      harness.detectChanges();
      expect(raiz.querySelector('.gestao-ligacoes > [role="alert"]')).toBeNull();
      http.expectOne(urlRemover).flush(null, { status: 204, statusText: 'No Content' });
    });

    it('não permite remover duas vezes enquanto decorre', async () => {
      const { harness, raiz, ligacoes } = await aConfirmar();
      sim(raiz);
      ligacoes.remover(ligacao(1));
      harness.detectChanges();
      expect(botao(raiz, 'A remover')?.disabled).toBeTrue();
      expect(raiz.querySelector<HTMLButtonElement>('#remover-l2')?.disabled).toBeTrue();
      http.expectOne(urlRemover).flush(null, { status: 204, statusText: 'No Content' });
    });
  });

  it('com a sessão expirada vai para a entrada ao guardar', async () => {
    const { harness, pagina } = await abrirEdicao();
    pagina.guardar();
    http.expectOne(urlPerfil).flush(null, { status: 401, statusText: 'Unauthorized' });
    await harness.fixture.whenStable();
    expect(TestBed.inject(SessaoService).autenticado()).toBeFalse();
  });
});
