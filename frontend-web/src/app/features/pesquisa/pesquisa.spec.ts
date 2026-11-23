import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';
import { CONFIGURACAO } from '../../core/configuracao';
import { autenticacaoInterceptor, erroInterceptor } from '../../core/interceptors';
import { SessaoService } from '../../core/sessao.service';
import { DetalheViagem, RotaDaViagem, ViagemEncontrada } from './pesquisa-api';

describe('I072 — pesquisa, detalhe, rota e custo', () => {
  const base = 'http://localhost:5100/api/v1';
  const id = '22222222-2222-4222-8222-222222222222';
  const urlPesquisa = base + '/pesquisa/viagens';
  const futuro = new Date(Date.now() + 7 * 24 * 3600 * 1000).toISOString();
  let http: HttpTestingController;

  const encontrada = (): ViagemEncontrada => ({
    id,
    origem: 'Porto',
    destino: 'Felgueiras',
    partidaEm: futuro,
    lugaresDisponiveis: 2,
    distanciaKm: 52.4,
    duracaoMinutos: 75,
    pontosAdicionais: 1,
    condutor: { id: 'condutor-1', nome: 'Ana Silva', classificacaoMedia: 4.5 },
  });
  const detalhe = (): DetalheViagem => ({
    ...encontrada(),
    estado: 'Ativa',
    condutorId: 'condutor-1',
    estimativaCusto: {
      custoTotalEstimado: 10,
      custoPorParticipante: 5,
      totalParticipantes: 2,
      moeda: 'EUR',
      indicativo: true,
    },
    estimativaIndisponivelMotivo: null,
  });
  const rota = (): RotaDaViagem => ({
    pontosOrdenados: [
      { ordem: 0, tipo: 'Origem', local: 'Porto', latitude: 41.15, longitude: -8.61 },
      { ordem: 1, tipo: 'DestinoViagem', local: 'Felgueiras', latitude: 41.36, longitude: -8.2 },
    ],
    distanciaTotalKm: 52.4,
    duracaoTotalMinutos: 75,
    geometria: [
      [41.15, -8.61],
      [41.36, -8.2],
    ],
    geometriaAproximada: true,
  });

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
    TestBed.inject(SessaoService).iniciar({
      accessToken: `a.${btoa(JSON.stringify({ sub: 'aluno-1', exp: Math.floor(Date.now() / 1000) + 3600 }))}.c`,
      expiresIn: 3600,
    });
  });
  afterEach(() => {
    http.verify();
    TestBed.inject(SessaoService).terminar();
  });

  async function abrir(url: string) {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url);
    harness.detectChanges();
    return { harness, raiz: harness.routeNativeElement as HTMLElement };
  }
  function preencher(raiz: HTMLElement, campo: string, valor: string) {
    const input = raiz.querySelector<HTMLInputElement>(`#${campo}`)!;
    input.value = valor;
    input.dispatchEvent(new Event('input'));
  }

  describe('pesquisa', () => {
    it('envia só os filtros preenchidos e mostra os resultados', async () => {
      const { harness, raiz } = await abrir('/pesquisa');
      preencher(raiz, 'origem', '  Porto ');
      preencher(raiz, 'data', '2030-05-10');
      raiz.querySelector('form')!.dispatchEvent(new Event('submit'));
      harness.detectChanges();
      expect(raiz.textContent).toContain('A pesquisar viagens');

      const pedido = http.expectOne((r) => r.url === urlPesquisa);
      expect(pedido.request.params.get('origem')).toBe('Porto');
      expect(pedido.request.params.get('data')).toBe('2030-05-10');
      expect(pedido.request.params.has('destino')).toBeFalse();
      expect(pedido.request.params.has('hora')).toBeFalse();
      expect(pedido.request.params.get('pagina')).toBe('1');
      pedido.flush({ total: 1, pagina: 1, tamanho: 20, viagens: [encontrada()] });
      harness.detectChanges();

      expect(raiz.textContent).toContain('1 viagens encontradas');
      expect(raiz.textContent).toContain('Ana Silva');
      expect(raiz.textContent).toContain('Classificação 4,5');
      expect(raiz.textContent).toContain('52,4 km');
      expect(raiz.querySelector('.viagens a')?.getAttribute('href')).toBe(`/pesquisa/${id}`);
    });

    it('mostra o estado vazio quando nenhuma viagem corresponde', async () => {
      const { harness, raiz } = await abrir('/pesquisa');
      raiz.querySelector('form')!.dispatchEvent(new Event('submit'));
      http
        .expectOne((r) => r.url === urlPesquisa)
        .flush({ total: 0, pagina: 1, tamanho: 20, viagens: [] });
      harness.detectChanges();
      expect(raiz.textContent).toContain('Nenhuma viagem corresponde aos filtros.');
    });

    it('mostra o erro { erro } devolvido pela API', async () => {
      const { harness, raiz } = await abrir('/pesquisa');
      raiz.querySelector('form')!.dispatchEvent(new Event('submit'));
      http
        .expectOne((r) => r.url === urlPesquisa)
        .flush({ erro: 'Paginação inválida.' }, { status: 400, statusText: 'Erro' });
      harness.detectChanges();
      expect(raiz.querySelector('[role=alert]')?.textContent).toContain('Paginação inválida.');
    });
  });

  describe('detalhe', () => {
    it('mostra o condutor, os lugares, o custo indicativo e a rota com os pontos', async () => {
      const { harness, raiz } = await abrir(`/pesquisa/${id}`);
      expect(raiz.textContent).toContain('A carregar a viagem');
      http.expectOne(`${base}/viagens/${id}`).flush(detalhe());
      harness.detectChanges();
      expect(raiz.textContent).toContain('A carregar a rota');
      http.expectOne(`${base}/rotas/viagens/${id}`).flush(rota());
      harness.detectChanges();

      expect(raiz.textContent).toContain('2 lugares disponíveis');
      expect(raiz.textContent).toContain('1 pontos adicionais');
      expect(raiz.textContent).toContain('Ana Silva');
      expect(raiz.textContent).toContain('Custo indicativo');
      expect(raiz.textContent).toContain('não é um preço');
      expect(raiz.textContent).toContain('A linha do mapa é aproximada');
      const pontos = [...raiz.querySelectorAll('app-mapa li')].map((li) => li.textContent);
      expect(pontos).toEqual(['Partida: Porto', 'Destino: Felgueiras']);
    });

    it('comunica a falha do serviço de mapas sem esconder o detalhe', async () => {
      const { harness, raiz } = await abrir(`/pesquisa/${id}`);
      http.expectOne(`${base}/viagens/${id}`).flush(detalhe());
      http
        .expectOne(`${base}/rotas/viagens/${id}`)
        .flush({ erro: 'Serviço de mapas indisponível.' }, { status: 503, statusText: 'Erro' });
      harness.detectChanges();

      expect(raiz.textContent).toContain('Felgueiras');
      expect(raiz.querySelector('[role=alert]')?.textContent).toContain(
        'A rota está indisponível: Serviço de mapas indisponível.',
      );
      expect(raiz.querySelector('app-mapa')).toBeNull();
    });
  });
});
