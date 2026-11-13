import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ApiClient } from '../../core/api-client';
import { CONFIGURACAO } from '../../core/configuracao';
import { AutenticacaoApiService } from './autenticacao-api.service';

describe('AutenticacaoApiService', () => {
  const naPagina = { erroNaPagina: true };
  let api: jasmine.SpyObj<ApiClient>;
  let servico: AutenticacaoApiService;

  beforeEach(() => {
    api = jasmine.createSpyObj<ApiClient>('ApiClient', ['post']);
    api.post.and.returnValue(of(null));
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: ApiClient, useValue: api },
        { provide: CONFIGURACAO, useValue: { apiBaseUrl: 'http://localhost:5100/api/v1' } },
      ],
    });
    servico = TestBed.inject(AutenticacaoApiService);
  });

  it('regista com nome, email e palavra-passe', () => {
    const dados = { nome: 'Ana', email: 'ana@estg.ipp.pt', password: 'Palavra1' };
    servico.registar(dados).subscribe();
    expect(api.post).toHaveBeenCalledOnceWith('autenticacao/registo', dados, naPagina);
  });

  it('pede o reenvio da confirmação', () => {
    servico.reenviarConfirmacao('ana@estg.ipp.pt').subscribe();
    expect(api.post).toHaveBeenCalledOnceWith(
      'autenticacao/reenviar-confirmacao',
      { email: 'ana@estg.ipp.pt' },
      naPagina,
    );
  });

  it('confirma a conta com o token da ligação', () => {
    servico.confirmar('abc').subscribe();
    expect(api.post).toHaveBeenCalledOnceWith('autenticacao/confirmar', { token: 'abc' }, naPagina);
  });
});
