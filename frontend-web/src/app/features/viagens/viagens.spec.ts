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
import { DetalhesViagem, ViagemDoCondutor } from '../../shared/models/viagem';
import {
  formatarPartida,
  formatarRota,
  paraCamposPortugal,
  paraPartidaUtc,
  partidaFutura,
} from './partida';

describe('I071 — viagens do condutor', () => {
  const base = 'http://localhost:5100/api/v1';
  const urlViagens = base + '/viagens';
  const urlMinhas = urlViagens + '/minhas';
  const id = '11111111-1111-4111-8111-111111111111';
  const urlViagem = `${urlViagens}/${id}`;
  const condutor = 'aluno-1';
  const futuro = new Date(Date.now() + 7 * 24 * 3600 * 1000).toISOString();
  const passado = new Date(Date.now() - 3600 * 1000).toISOString();

  const detalhes = (alteracoes: Partial<DetalhesViagem> = {}): DetalhesViagem => ({
    id,
    origem: 'Porto',
    destino: 'Felgueiras',
    partidaEm: futuro,
    lugaresDisponiveis: 3,
    estado: 'Ativa',
    distanciaKm: 52.4,
    duracaoMinutos: 75,
    condutorId: condutor,
    ...alteracoes,
  });
  const resumo = (alteracoes: Partial<ViagemDoCondutor> = {}): ViagemDoCondutor => ({
    id,
    origem: 'Porto',
    destino: 'Felgueiras',
    partidaEm: futuro,
    lugaresOferecidos: 4,
    lugaresDisponiveis: 3,
    ...alteracoes,
  });

  let http: HttpTestingController;
  let ui: NotificacaoUiService;
  const recusar = (url: string, status: number, erro: string) =>
    http.expectOne(url).flush({ erro }, { status, statusText: 'Erro' });
  const botao = (raiz: HTMLElement, texto: string) =>
    [...raiz.querySelectorAll('button')].find((b) => b.textContent?.includes(texto));
  const preencher = (raiz: HTMLElement, id: string, valor: string) => {
    const campo = raiz.querySelector<HTMLInputElement>(`#${id}`)!;
    campo.value = valor;
    campo.dispatchEvent(new Event('input'));
    campo.dispatchEvent(new Event('blur'));
  };

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
      accessToken: `a.${btoa(JSON.stringify({ sub: condutor, exp: Math.floor(Date.now() / 1000) + 3600 }))}.c`,
      expiresIn: 3600,
    });
  });
  afterEach(() => {
    http.verify();
    TestBed.inject(SessaoService).terminar();
  });

  describe('funções auxiliares', () => {
    it('converte a hora de Portugal para UTC com Z e de volta', () => {
      const utc = paraPartidaUtc('2030-05-10', '10:30');
      expect(utc).toBe('2030-05-10T09:30:00.000Z');
      expect(paraCamposPortugal(utc)).toEqual({ data: '2030-05-10', hora: '10:30' });
    });
    it('RNF-FIA-04: 09:00 de Lisboa é 09:00 no inverno e no verão', () => {
      expect(paraPartidaUtc('2030-01-15', '09:00')).toBe('2030-01-15T09:00:00.000Z');
      expect(paraPartidaUtc('2030-07-15', '09:00')).toBe('2030-07-15T08:00:00.000Z');
      expect(formatarPartida('2030-01-15T09:00:00Z')).toContain('09:00');
      expect(formatarPartida('2030-07-15T08:00:00Z')).toContain('09:00');
      expect(paraCamposPortugal('2030-07-15T08:00:00Z').hora).toBe('09:00');
    });
    it('passa de dia na hora de Portugal e mostra a meia-noite como 00', () => {
      expect(paraCamposPortugal('2030-06-30T23:30:00Z')).toEqual({
        data: '2030-07-01',
        hora: '00:30',
      });
    });
    it('nas mudanças de hora, usa a primeira hora repetida e avança a que não existe', () => {
      expect(paraPartidaUtc('2030-10-27', '01:30')).toBe('2030-10-27T00:30:00.000Z');
      expect(paraPartidaUtc('2030-10-27', '03:00')).toBe('2030-10-27T03:00:00.000Z');
      expect(paraPartidaUtc('2030-03-31', '01:30')).toBe('2030-03-31T01:30:00.000Z');
      expect(paraPartidaUtc('2030-03-31', '09:00')).toBe('2030-03-31T08:00:00.000Z');
    });
    it('formata a rota com os dados que existirem', () => {
      expect(formatarRota(52.4, 75)).toBe('52,4 km · cerca de 1 h 15 min');
      expect(formatarRota(null, 25)).toBe('cerca de 25 min');
      expect(formatarRota(10, null)).toBe('10,0 km');
      expect(formatarRota(null, null)).toBe('');
    });
    it('o validador da partida deixa os campos vazios para o required', () => {
      const validador = partidaFutura('data');
      expect(validador({ parent: null, value: '10:00' } as never)).toBeNull();
    });
  });

  describe('lista', () => {
    async function abrir() {
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl('/viagens');
      harness.detectChanges();
      return { harness, raiz: harness.routeNativeElement as HTMLElement };
    }

    it('mostra o carregamento e depois o estado e os lugares de cada viagem', async () => {
      const { harness, raiz } = await abrir();
      expect(raiz.querySelector('[role=status]')?.textContent).toContain('A carregar');
      http.expectOne(urlMinhas).flush([resumo()]);
      harness.detectChanges();
      expect(raiz.textContent).toContain('Porto');
      expect(raiz.textContent).toContain('Ativa');
      expect(raiz.textContent).toContain('3 de 4 lugares disponíveis');
      expect(raiz.querySelector('.viagens a')?.getAttribute('href')).toBe(`/viagens/${id}/editar`);
    });

    it('não oferece a eliminação permanente', async () => {
      const { harness, raiz } = await abrir();
      http.expectOne(urlMinhas).flush([resumo()]);
      harness.detectChanges();
      expect(raiz.textContent?.toLowerCase()).not.toContain('eliminar');
    });

    it('explica quando não há viagens e deixa criar uma', async () => {
      const { harness, raiz } = await abrir();
      http.expectOne(urlMinhas).flush([]);
      harness.detectChanges();
      expect(raiz.textContent).toContain('Ainda não tens viagens ativas.');
      expect(raiz.querySelector('a.primary')?.getAttribute('href')).toBe('/viagens/nova');
    });

    it('mostra o erro e volta a tentar', async () => {
      const { harness, raiz } = await abrir();
      recusar(urlMinhas, 500, 'x');
      harness.detectChanges();
      expect(raiz.querySelector('[role=alert]')?.textContent).toContain('erro no servidor');
      botao(raiz, 'Tentar novamente')!.click();
      http.expectOne(urlMinhas).flush([]);
      harness.detectChanges();
      expect(raiz.querySelector('[role=alert]')).toBeNull();
    });
  });

  describe('criação', () => {
    async function abrir() {
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl('/viagens/nova');
      harness.detectChanges();
      return { harness, raiz: harness.routeNativeElement as HTMLElement };
    }
    function preencherValido(raiz: HTMLElement) {
      preencher(raiz, 'origem', '  Porto ');
      preencher(raiz, 'destino', 'Felgueiras');
      preencher(raiz, 'data', '2030-05-10');
      preencher(raiz, 'hora', '10:30');
      preencher(raiz, 'lugares', '3');
    }
    const submeter = (raiz: HTMLElement) => botao(raiz, 'Criar viagem')!.click();

    it('envia a viagem em UTC, mostra a rota e volta à lista', async () => {
      const { harness, raiz } = await abrir();
      preencherValido(raiz);
      submeter(raiz);
      harness.detectChanges();
      const pedido = http.expectOne(urlViagens);
      expect(pedido.request.method).toBe('POST');
      expect(pedido.request.body).toEqual({
        origem: 'Porto',
        destino: 'Felgueiras',
        partidaEm: '2030-05-10T09:30:00.000Z',
        lugares: 3,
      });
      expect(botao(raiz, 'A criar a viagem…')?.disabled).toBeTrue();
      pedido.flush({ id, distanciaKm: 52.4, duracaoMinutos: 75 });
      await harness.fixture.whenStable();
      expect(TestBed.inject(Router).url).toBe('/viagens');
      expect(ui.mensagem()?.texto).toBe('Viagem criada. Rota: 52,4 km · cerca de 1 h 15 min.');
      http.expectOne(urlMinhas).flush([resumo()]);
    });

    it('não menciona a rota quando a API não a devolve', async () => {
      const { harness, raiz } = await abrir();
      preencherValido(raiz);
      submeter(raiz);
      http.expectOne(urlViagens).flush({ id, distanciaKm: null, duracaoMinutos: null });
      await harness.fixture.whenStable();
      expect(ui.mensagem()?.texto).toBe('Viagem criada.');
      http.expectOne(urlMinhas).flush([]);
    });

    it('não oferece recorrência nem dados do veículo (RR-05, RR-10)', async () => {
      const { raiz } = await abrir();
      const ids = [...raiz.querySelectorAll('input')].map((campo) => campo.id);
      expect(ids).toEqual(['origem', 'destino', 'data', 'hora', 'lugares']);
    });

    it('rejeita dados inválidos sem chamar a API', async () => {
      const { harness, raiz } = await abrir();
      preencher(raiz, 'lugares', '');
      submeter(raiz);
      harness.detectChanges();
      for (const texto of [
        'Indica a origem.',
        'Indica o destino.',
        'Indica a data da viagem.',
        'Indica a hora da viagem.',
        'Indica o número de lugares.',
      ])
        expect(raiz.textContent).toContain(texto);
      expect(raiz.querySelector('#origem')?.getAttribute('aria-invalid')).toBe('true');
    });

    it('rejeita origem e destino iguais, data passada e lugares fora de 1 a 8', async () => {
      const { harness, raiz } = await abrir();
      preencherValido(raiz);
      preencher(raiz, 'destino', ' PORTO');
      preencher(raiz, 'data', '2020-01-01');
      preencher(raiz, 'lugares', '9');
      submeter(raiz);
      harness.detectChanges();
      expect(raiz.textContent).toContain('A origem e o destino não podem ser o mesmo local.');
      expect(raiz.textContent).toContain('posteriores ao momento atual');
      expect(raiz.textContent).toContain('O máximo é 8 lugares.');
      preencher(raiz, 'lugares', '0');
      harness.detectChanges();
      expect(raiz.textContent).toContain('Indica pelo menos 1 lugar.');
      preencher(raiz, 'lugares', '1.5');
      harness.detectChanges();
      expect(raiz.textContent).toContain('Indica um número inteiro de lugares.');
    });

    it('revalida o destino quando a origem muda', async () => {
      const { harness, raiz } = await abrir();
      preencher(raiz, 'destino', 'Porto');
      preencher(raiz, 'origem', 'Porto');
      harness.detectChanges();
      expect(raiz.textContent).toContain('mesmo local');
      preencher(raiz, 'origem', 'Braga');
      harness.detectChanges();
      expect(raiz.textContent).not.toContain('mesmo local');
    });

    it('mostra a mensagem do serviço de mapas e mantém os dados (RF-F002-17)', async () => {
      const { harness, raiz } = await abrir();
      preencherValido(raiz);
      submeter(raiz);
      recusar(urlViagens, 503, 'Serviço de rotas indisponível.');
      await harness.fixture.whenStable();
      harness.detectChanges();
      expect(raiz.querySelector('[role=alert]')?.textContent).toContain(
        'Serviço de rotas indisponível.',
      );
      expect(raiz.querySelector<HTMLInputElement>('#origem')?.value).toBe('  Porto ');
      expect(TestBed.inject(Router).url).toBe('/viagens/nova');
      expect(ui.mensagem()).toBeNull();
    });
  });

  describe('gestão', () => {
    async function abrir(
      viagem = detalhes(),
      minhas: ViagemDoCondutor[] = [resumo()],
      url = `/viagens/${id}/editar`,
    ) {
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl(url);
      harness.detectChanges();
      http.expectOne(urlViagem).flush(viagem);
      http.expectOne(urlMinhas).flush(minhas);
      harness.detectChanges();
      await harness.fixture.whenStable();
      harness.detectChanges();
      return { harness, raiz: harness.routeNativeElement as HTMLElement };
    }

    it('apresenta o estado, os lugares e a rota, com o formulário preenchido', async () => {
      const { raiz } = await abrir();
      expect(raiz.textContent).toContain('Ativa');
      expect(raiz.textContent).toContain('3 de 4');
      expect(raiz.textContent).toContain('52,4 km');
      expect(raiz.querySelector<HTMLInputElement>('#origem')?.value).toBe('Porto');
      expect(raiz.querySelector<HTMLInputElement>('#lugares')?.value).toBe('4');
      expect(raiz.querySelector<HTMLInputElement>('#data')?.value).toBe(
        paraCamposPortugal(futuro).data,
      );
      expect(raiz.textContent?.toLowerCase()).not.toContain('eliminar');
    });

    it('guarda as alterações e volta a ler os dados da API', async () => {
      const { harness, raiz } = await abrir();
      preencher(raiz, 'lugares', '5');
      botao(raiz, 'Guardar alterações')!.click();
      const pedido = http.expectOne(urlViagem);
      expect(pedido.request.method).toBe('PUT');
      expect(pedido.request.body.lugares).toBe(5);
      expect(pedido.request.body.partidaEm).toBe(
        paraPartidaUtc(paraCamposPortugal(futuro).data, paraCamposPortugal(futuro).hora),
      );
      pedido.flush(null, { status: 204, statusText: 'No Content' });
      http.expectOne(urlViagem).flush(detalhes({ lugaresDisponiveis: 4 }));
      http.expectOne(urlMinhas).flush([resumo({ lugaresOferecidos: 5, lugaresDisponiveis: 4 })]);
      harness.detectChanges();
      await harness.fixture.whenStable();
      expect(ui.mensagem()?.texto).toBe('Viagem atualizada.');
      expect(raiz.textContent).toContain('4 de 5');
      expect(document.activeElement?.id).toBe('titulo-viagem');
    });

    it('não deixa oferecer menos lugares do que os ocupados (RF-F002-08)', async () => {
      const { harness, raiz } = await abrir(detalhes({ lugaresDisponiveis: 1 }));
      expect(raiz.textContent).toContain('De 3 a 8.');
      preencher(raiz, 'lugares', '2');
      botao(raiz, 'Guardar alterações')!.click();
      harness.detectChanges();
      expect(raiz.textContent).toContain('Já tens 3 lugares ocupados');
    });

    it('um erro de validação da API mantém o formulário e os dados', async () => {
      const { harness, raiz } = await abrir();
      preencher(raiz, 'origem', 'Braga');
      botao(raiz, 'Guardar alterações')!.click();
      recusar(urlViagem, 422, 'Origem ou destino não encontrado.');
      await harness.fixture.whenStable();
      harness.detectChanges();
      expect(raiz.querySelector('[role=alert]')?.textContent).toContain('não encontrado');
      expect(raiz.querySelector<HTMLInputElement>('#origem')?.value).toBe('Braga');
    });

    it('um conflito atualiza a viagem e mantém a mensagem', async () => {
      const { harness, raiz } = await abrir();
      botao(raiz, 'Guardar alterações')!.click();
      recusar(urlViagem, 409, 'Viagem não editável.');
      http.expectOne(urlViagem).flush(detalhes({ estado: 'Concluida' }));
      http.expectOne(urlMinhas).flush([]);
      harness.detectChanges();
      await harness.fixture.whenStable();
      harness.detectChanges();
      expect(raiz.textContent).toContain('Concluída');
      expect(raiz.textContent).toContain('Viagem não editável.');
      expect(raiz.querySelector('form')).toBeNull();
    });

    it('não deixa alterar uma viagem que já partiu, mas deixa indisponibilizar', async () => {
      const { raiz } = await abrir(detalhes({ partidaEm: passado }), [
        resumo({ partidaEm: passado }),
      ]);
      expect(raiz.textContent).toContain('já não pode ser alterada');
      expect(raiz.querySelector('form')).toBeNull();
      expect(botao(raiz, 'Indisponibilizar')).toBeDefined();
    });

    it('sem ações numa viagem indisponibilizada; mostra só os lugares conhecidos', async () => {
      const { raiz } = await abrir(detalhes({ estado: 'Indisponibilizada' }), []);
      expect(raiz.textContent).toContain('Indisponibilizada');
      expect(raiz.textContent).toContain('3');
      expect(raiz.textContent).not.toContain('3 de');
      expect(raiz.querySelector('form')).toBeNull();
      expect(botao(raiz, 'Indisponibilizar')).toBeUndefined();
    });

    it('omite a rota quando a API não a tem', async () => {
      const { raiz } = await abrir(detalhes({ distanciaKm: null, duracaoMinutos: null }));
      expect(raiz.textContent).not.toContain('Rota');
    });

    it('pede confirmação, avisa dos cancelamentos e indisponibiliza', async () => {
      const { harness, raiz } = await abrir();
      botao(raiz, 'Indisponibilizar')!.click();
      harness.detectChanges();
      await harness.fixture.whenStable();
      expect(raiz.textContent).toContain('serão cancelados');
      expect(document.activeElement?.id).toBe('manter-viagem');
      botao(raiz, 'Sim, indisponibilizar')!.click();
      const pedido = http.expectOne(`${urlViagem}/indisponibilizar`);
      expect(pedido.request.method).toBe('POST');
      pedido.flush(null, { status: 204, statusText: 'No Content' });
      http.expectOne(urlViagem).flush(detalhes({ estado: 'Indisponibilizada' }));
      http.expectOne(urlMinhas).flush([]);
      harness.detectChanges();
      await harness.fixture.whenStable();
      harness.detectChanges();
      expect(ui.mensagem()?.texto).toBe('Viagem indisponibilizada.');
      expect(raiz.textContent).toContain('Indisponibilizada');
    });

    it('permite desistir da indisponibilização', async () => {
      const { harness, raiz } = await abrir();
      botao(raiz, 'Indisponibilizar')!.click();
      harness.detectChanges();
      botao(raiz, 'Não')!.click();
      harness.detectChanges();
      await harness.fixture.whenStable();
      expect(document.activeElement?.id).toBe('indisponibilizar-viagem');
      expect(raiz.textContent).not.toContain('serão cancelados');
    });

    it('mostra a falha da indisponibilização junto do botão', async () => {
      const { harness, raiz } = await abrir();
      botao(raiz, 'Indisponibilizar')!.click();
      harness.detectChanges();
      botao(raiz, 'Sim, indisponibilizar')!.click();
      recusar(`${urlViagem}/indisponibilizar`, 500, 'x');
      await harness.fixture.whenStable();
      harness.detectChanges();
      expect(raiz.querySelector('.zona-indisponibilizar [role=alert]')?.textContent).toContain(
        'erro no servidor',
      );
    });

    it('uma viagem que já não está ativa atualiza o estado e explica porquê', async () => {
      const { harness, raiz } = await abrir();
      botao(raiz, 'Indisponibilizar')!.click();
      harness.detectChanges();
      botao(raiz, 'Sim, indisponibilizar')!.click();
      recusar(`${urlViagem}/indisponibilizar`, 409, 'A viagem já não está ativa.');
      http.expectOne(urlViagem).flush(detalhes({ estado: 'Indisponibilizada' }));
      http.expectOne(urlMinhas).flush([]);
      harness.detectChanges();
      await harness.fixture.whenStable();
      harness.detectChanges();
      expect(raiz.textContent).toContain('A viagem já não está ativa.');
    });

    it('recusa a viagem de outro condutor sem mostrar ações', async () => {
      const { raiz } = await abrir(detalhes({ condutorId: 'outra-pessoa' }));
      expect(raiz.querySelector('[role=alert]')?.textContent).toContain('Só o condutor');
      expect(raiz.querySelector('form')).toBeNull();
      expect(botao(raiz, 'Tentar novamente')).toBeUndefined();
    });

    it('um identificador mal formado não chama a API', async () => {
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl('/viagens/a%20b/editar');
      harness.detectChanges();
      const raiz = harness.routeNativeElement as HTMLElement;
      expect(raiz.querySelector('[role=alert]')?.textContent).toContain('Viagem não encontrada.');
    });

    it('404 não oferece repetir; outros erros sim', async () => {
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl(`/viagens/${id}/editar`);
      harness.detectChanges();
      const raiz = harness.routeNativeElement as HTMLElement;
      recusar(urlViagem, 500, 'x');
      expect(http.expectOne(urlMinhas).cancelled).toBeTrue();
      harness.detectChanges();
      expect(botao(raiz, 'Tentar novamente')).toBeDefined();
      botao(raiz, 'Tentar novamente')!.click();
      recusar(urlViagem, 404, 'Viagem não encontrada.');
      expect(http.expectOne(urlMinhas).cancelled).toBeTrue();
      harness.detectChanges();
      expect(raiz.querySelector('[role=alert]')?.textContent).toContain('Viagem não encontrada.');
      expect(botao(raiz, 'Tentar novamente')).toBeUndefined();
    });
  });
});
