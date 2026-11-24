import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute, convertToParamMap } from '@angular/router';
import { Subject } from 'rxjs';
import { CONFIGURACAO } from '../core/configuracao';
import { SessaoService } from '../core/sessao.service';
import { PesquisaPage } from './pesquisa/pesquisa.page';
import { ParticipacoesPage } from './participacoes/participacoes.page';
import { MensagensPage } from './comunicacao/mensagens.page';
import { NotificacoesPage } from './notificacoes/notificacoes.page';
import { HistoricoPage } from './historico/historico.page';
import { TempoRealService } from './notificacoes/tempo-real.service';
const url = 'https://api.example.test/api/v1/';
describe('Integração frontend M4 com contrato HTTP simulado', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: CONFIGURACAO, useValue: { apiBaseUrl: url.slice(0, -1) } },
        {
          provide: SessaoService,
          useValue: {
            utilizador: signal({ id: 'eu' }),
            autenticado: signal(true),
            obterToken: () => null,
          },
        },
        {
          provide: TempoRealService,
          useValue: { estado: signal('Ligado'), alteracoes: new Subject<void>() },
        },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({}), queryParamMap: convertToParamMap({}) },
          },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });
  it('pesquisa codifica filtros e apresenta resultado vazio sem inventar viagens', () => {
    const f = TestBed.createComponent(PesquisaPage);
    f.componentInstance.filtro = {
      origem: ' Porto & Gaia ',
      destino: 'Campus',
      data: '2026-11-20',
      hora: '17:00',
      pagina: 1,
    };
    f.componentInstance.procurar(1);
    const r = http.expectOne((x) => x.url === url + 'pesquisa/viagens');
    expect(r.request.params.get('origem')).toBe('Porto & Gaia');
    expect(r.request.params.get('data')).toBe('2026-11-20');
    r.flush({ total: 0, viagens: [] });
    f.detectChanges();
    expect(f.nativeElement.textContent).toContain('0 viagens encontradas');
  });
  it('impede submissões simultâneas na pesquisa e mantém o erro visível', () => {
    const f = TestBed.createComponent(PesquisaPage);
    f.componentInstance.procurar(1);
    f.componentInstance.procurar(1);
    const r = http.expectOne((x) => x.url.endsWith('/pesquisa/viagens'));
    r.error(new ProgressEvent('error'));
    expect(f.componentInstance.ocupado()).toBeFalse();
    expect(f.componentInstance.erro()).not.toBe('');
  });
  it('não envia aceitação antes de uma simulação do mesmo pedido', () => {
    const f = TestBed.createComponent(ParticipacoesPage);
    http.expectOne(url + 'historico').flush({ pedidosEfetuados: [] });
    f.componentInstance.decidir('pedido', 'aceitar');
    http.expectNone(url + 'participacoes/pedido/aceitar');
  });
  it('mantém o texto da mensagem quando o envio falha', async () => {
    const f = TestBed.createComponent(MensagensPage);
    http.expectOne(url + 'mensagens/conversas').flush([]);
    f.componentInstance.abrir('colega', 'Colega');
    http.expectOne(url + 'mensagens/conversa/colega').flush([]);
    f.componentInstance.conteudo = 'Mensagem que deve ser preservada';
    f.componentInstance.enviar();
    const r = http.expectOne(url + 'mensagens');
    expect(r.request.body).toEqual({
      destinatarioId: 'colega',
      conteudo: 'Mensagem que deve ser preservada',
    });
    r.flush({}, { status: 503, statusText: 'Indisponível' });
    expect(f.componentInstance.conteudo).toContain('preservada');
  });
  it('escapa o conteúdo recebido em vez de executar HTML', () => {
    const f = TestBed.createComponent(MensagensPage);
    http.expectOne(url + 'mensagens/conversas').flush([]);
    f.componentInstance.abrir('colega', 'Colega');
    http.expectOne(url + 'mensagens/conversa/colega').flush([
      {
        id: 'm1',
        remetenteId: 'colega',
        conteudo: '<img src=x onerror=alert(1)>',
        enviadoEm: '2026-11-14T10:00:00Z',
      },
    ]);
    f.detectChanges();
    expect(f.nativeElement.querySelector('article img')).toBeNull();
    expect(f.nativeElement.textContent).toContain('<img');
  });
  it('marca uma notificação como lida e volta a obter o estado do servidor', async () => {
    const f = TestBed.createComponent(NotificacoesPage);
    http
      .expectOne((x) => x.url === url + 'notificacoes')
      .flush({ itens: [], pagina: 1, totalPaginas: 1, naoLidas: 1 });
    f.componentInstance.ler('n1');
    const r = http.expectOne(url + 'notificacoes/n1/ler');
    expect(r.request.method).toBe('PUT');
    r.flush(null, { status: 204, statusText: 'No Content' });
    await Promise.resolve();
    http
      .expectOne((x) => x.url === url + 'notificacoes')
      .flush({ itens: [], pagina: 1, totalPaginas: 1, naoLidas: 0 });
    expect(f.componentInstance.caixa()?.naoLidas).toBe(0);
  });
  it('recusa classificações não inteiras ou fora de 1 a 5', () => {
    const f = TestBed.createComponent(HistoricoPage);
    http.expectOne(url + 'historico').flush({
      viagensComoCondutor: [],
      viagensComoPassageiro: [],
      pedidosEfetuados: [],
      pedidosRecebidos: [],
      notificacoes: [],
      avisos: [],
    });
    f.componentInstance.viagem.set('v1');
    f.componentInstance.pessoas.set([{ id: 'p1', nome: 'Colega' }]);
    f.componentInstance.avaliadoId = 'p1';
    for (const valor of [0, 6, 2.5]) {
      f.componentInstance.classificacao = valor;
      f.componentInstance.avaliar();
    }
    http.expectNone(url + 'avaliacoes');
  });
  it('publica avaliação apenas para um participante da viagem selecionada', () => {
    const f = TestBed.createComponent(HistoricoPage);
    http.expectOne(url + 'historico').flush({
      viagensComoCondutor: [],
      viagensComoPassageiro: [],
      pedidosEfetuados: [],
      pedidosRecebidos: [],
      notificacoes: [],
      avisos: [],
    });
    f.componentInstance.viagem.set('v1');
    f.componentInstance.pessoas.set([{ id: 'p1', nome: 'Colega' }]);
    f.componentInstance.avaliadoId = 'p1';
    f.componentInstance.classificacao = 4;
    f.componentInstance.avaliar();
    const r = http.expectOne(url + 'avaliacoes');
    expect(r.request.body).toEqual({ viagemId: 'v1', avaliadoId: 'p1', classificacao: 4 });
    r.flush({ id: 'a1' });
    expect(f.componentInstance.aviso()).toContain('guardada');
  });
});
