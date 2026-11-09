import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiClient } from '../../core/api-client';
import { PerfilApiService } from './perfil-api.service';

describe('PerfilApiService', () => {
  const naPagina = { erroNaPagina: true };
  let api: jasmine.SpyObj<ApiClient>;
  let servico: PerfilApiService;

  beforeEach(() => {
    api = jasmine.createSpyObj<ApiClient>('ApiClient', ['get', 'post', 'put', 'delete']);
    api.get.and.returnValue(of(null));
    api.post.and.returnValue(of(null));
    api.put.and.returnValue(of(null));
    api.delete.and.returnValue(of(null));
    TestBed.configureTestingModule({ providers: [{ provide: ApiClient, useValue: api }] });
    servico = TestBed.inject(PerfilApiService);
  });

  it('obtém o perfil próprio', () => {
    servico.obterProprio().subscribe();
    expect(api.get).toHaveBeenCalledOnceWith('utilizadores/me', undefined, naPagina);
  });

  it('atualiza o perfil próprio', () => {
    const dados = { nome: 'Ana', curso: null, instituicao: 'ESTG' };
    servico.atualizar(dados).subscribe();
    expect(api.put).toHaveBeenCalledOnceWith('utilizadores/me', dados, naPagina);
  });

  it('acrescenta uma ligação', () => {
    const ligacao = { url: 'https://exemplo.pt', descricao: null };
    servico.adicionarLigacao(ligacao).subscribe();
    expect(api.post).toHaveBeenCalledOnceWith('utilizadores/me/ligacoes', ligacao, naPagina);
  });

  it('remove uma ligação', () => {
    servico.removerLigacao('l1').subscribe();
    expect(api.delete).toHaveBeenCalledOnceWith('utilizadores/me/ligacoes/l1', naPagina);
  });

  it('obtém o perfil público pelo id', () => {
    servico.obterPublico('u1').subscribe();
    expect(api.get).toHaveBeenCalledOnceWith('utilizadores/u1', undefined, naPagina);
  });

  it('codifica o id para não poder alterar o caminho do pedido', () => {
    servico.obterPublico('../me').subscribe();
    expect(api.get).toHaveBeenCalledOnceWith('utilizadores/..%2Fme', undefined, naPagina);
  });
});
