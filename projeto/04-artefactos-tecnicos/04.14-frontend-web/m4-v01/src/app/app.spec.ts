import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { App } from './app';
import { routes } from './app.routes';
import { CONFIGURACAO } from './core/configuracao';
import { SessaoService } from './core/sessao.service';
import { autenticacaoInterceptor, erroInterceptor } from './core/interceptors';
import { LoginPage } from './features/autenticacao/login.page';
import { PerfilPage } from './features/perfil/perfil.page';
import { InicioPage } from './features/inicio/inicio.page';
import { NotificacaoUiService } from './core/notificacao-ui.service';

describe('I069 — interface', () => {
  const base = 'http://localhost:5100/api/v1';
  const perfil = {
    id: 'aluno-1',
    nome: 'Ana Silva',
    email: 'ana@alunos.test',
    emailConfirmado: true,
    curso: null,
    instituicao: null,
    criadoEm: '2026-11-01',
    ligacoesExternas: [],
    classificacaoMedia: null,
    totalAvaliacoes: 0,
  };
  function iniciar() {
    TestBed.inject(SessaoService).iniciar({
      accessToken: `a.${btoa(JSON.stringify({ sub: 'aluno-1', exp: Math.floor(Date.now() / 1000) + 3600 }))}.c`,
      expiresIn: 3600,
    });
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
  });
  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.resetTestingModule();
    sessionStorage.clear();
  });

  describe('Páginas, navegação e sessão', () => {
    it('redireciona uma ligação direta protegida para login preservando o regresso', async () => {
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl('/perfil', LoginPage);
      expect(TestBed.inject(Router).url).toBe('/entrar?regresso=%2Fperfil');
      expect(harness.routeNativeElement?.textContent).toContain('Email académico');
    });
    it('envia visitantes autenticados para início e apresenta uma página 404', async () => {
      iniciar();
      const h = await RouterTestingHarness.create();
      await h.navigateByUrl('/entrar', InicioPage);
      expect(TestBed.inject(Router).url).toBe('/inicio');
      await h.navigateByUrl('/desconhecida');
      expect(h.routeNativeElement?.textContent).toContain('Este caminho não existe');
    });
    it('valida campos vazios e não envia credenciais inválidas', async () => {
      const h = await RouterTestingHarness.create();
      const page = await h.navigateByUrl('/entrar', LoginPage);
      page.entrar();
      h.detectChanges();
      expect(h.routeNativeElement?.textContent).toContain('Introduz um email válido');
      expect(h.routeNativeElement?.textContent).toContain('Introduz a palavra-passe');
      TestBed.inject(HttpTestingController).expectNone(base + '/autenticacao/login');
    });
    it('mostra os erros de validação da API e limpa a palavra-passe', async () => {
      const h = await RouterTestingHarness.create();
      const page = await h.navigateByUrl('/entrar', LoginPage);
      page.formulario.setValue({ email: 'ana@alunos.test', password: 'teste' });
      page.entrar();
      page.entrar();
      const http = TestBed.inject(HttpTestingController),
        req = http.expectOne(base + '/autenticacao/login');
      req.flush(
        {
          erro: {
            mensagem: 'Dados inválidos.',
            campos: { Email: ['Email não permitido.'], Password: ['Palavra-passe inválida.'] },
          },
        },
        { status: 400, statusText: 'Bad Request' },
      );
      h.detectChanges();
      expect(page.aEntrar()).toBeFalse();
      expect(page.formulario.controls.password.value).toBe('');
      expect(h.routeNativeElement?.textContent).toContain('Email não permitido');
      expect(h.routeNativeElement?.textContent).toContain('Palavra-passe inválida');
    });
    it('conclui login e navega sem persistir a palavra-passe', async () => {
      const h = await RouterTestingHarness.create();
      const page = await h.navigateByUrl('/entrar', LoginPage);
      page.formulario.setValue({ email: 'ana@alunos.test', password: 'segredo-de-teste' });
      page.entrar();
      const http = TestBed.inject(HttpTestingController);
      http.expectOne(base + '/autenticacao/login').flush({
        accessToken: `a.${btoa(JSON.stringify({ sub: 'aluno-1', exp: Math.floor(Date.now() / 1000) + 3600 }))}.c`,
        expiresIn: 3600,
      });
      await h.fixture.whenStable();
      expect(TestBed.inject(Router).url).toBe('/inicio');
      expect(JSON.stringify(sessionStorage)).not.toContain('segredo-de-teste');
    });
    it('limpa a notificação ao navegar, exceto a que antecede um redirecionamento', async () => {
      const ui = TestBed.inject(NotificacaoUiService),
        router = TestBed.inject(Router);
      ui.erro('Antiga');
      await router.navigateByUrl('/entrar');
      expect(ui.mensagem()).toBeNull();
      ui.sucesso('Sessão terminada.', { manterAoNavegar: true });
      await router.navigateByUrl('/');
      expect(ui.mensagem()?.texto).toBe('Sessão terminada.');
      await router.navigateByUrl('/entrar');
      expect(ui.mensagem()).toBeNull();
    });
    it('o atalho de acessibilidade foca o conteúdo sem sair da página', async () => {
      const f = TestBed.createComponent(App);
      f.detectChanges();
      await TestBed.inject(Router).navigateByUrl('/entrar');
      await f.whenStable();
      f.nativeElement.querySelector('.skip-link').click();
      expect(document.activeElement?.id).toBe('conteudo');
      expect(TestBed.inject(Router).url).toBe('/entrar');
    });
    it('apresenta carregamento, perfil e estados sem dados opcionais', async () => {
      iniciar();
      const h = await RouterTestingHarness.create();
      await h.navigateByUrl('/perfil', PerfilPage);
      expect(h.routeNativeElement?.textContent).toContain('A carregar');
      TestBed.inject(HttpTestingController)
        .expectOne(base + '/utilizadores/me')
        .flush(perfil);
      h.detectChanges();
      expect(h.routeNativeElement?.textContent).toContain('Ana Silva');
      expect(h.routeNativeElement?.textContent).toContain('Não indicado');
      expect(h.routeNativeElement?.textContent).toContain('Sem avaliações');
    });
    it('permite repetir a consulta após erro de rede', async () => {
      iniciar();
      const h = await RouterTestingHarness.create();
      const page = await h.navigateByUrl('/perfil', PerfilPage),
        http = TestBed.inject(HttpTestingController);
      page.carregar();
      http.expectOne(base + '/utilizadores/me').error(new ProgressEvent('error'));
      h.detectChanges();
      expect(h.routeNativeElement?.textContent).toContain('Tentar novamente');
      page.carregar();
      http
        .expectOne(base + '/utilizadores/me')
        .flush({ ...perfil, curso: 'Informática', instituicao: 'IPCA', classificacaoMedia: 4.5 });
      h.detectChanges();
      expect(h.routeNativeElement?.textContent).toContain('4.5 / 5');
    });
    it('apresenta navegação pública e notificações acessíveis', async () => {
      const f = TestBed.createComponent(App);
      f.detectChanges();
      await TestBed.inject(Router).navigateByUrl('/');
      await f.whenStable();
      expect(f.nativeElement.textContent).toContain('Entrar na comunidade');
      expect(f.nativeElement.querySelector('nav')).toBeNull();
      TestBed.inject(NotificacaoUiService).erro('Falha de ligação');
      f.detectChanges();
      expect(f.nativeElement.querySelector('[role="alert"]').textContent).toContain('Falha');
      f.nativeElement.querySelector('[aria-label="Fechar mensagem"]').click();
      f.detectChanges();
      expect(f.nativeElement.querySelector('[role="alert"]')).toBeNull();
    });
    it('valida a sessão restaurada e termina-a pelo botão do cabeçalho', async () => {
      iniciar();
      const f = TestBed.createComponent(App);
      f.detectChanges();
      const http = TestBed.inject(HttpTestingController);
      http.expectOne(base + '/autenticacao/me').flush({ id: 'aluno-1' });
      await TestBed.inject(Router).navigateByUrl('/inicio');
      await f.whenStable();
      expect(f.nativeElement.querySelector('nav').textContent).toContain('Perfil');
      f.componentInstance.sair();
      f.componentInstance.sair();
      http
        .expectOne(base + '/autenticacao/logout')
        .flush(null, { status: 204, statusText: 'No Content' });
      f.detectChanges();
      await f.whenStable();
      expect(TestBed.inject(SessaoService).autenticado()).toBeFalse();
      expect(TestBed.inject(Router).url).toBe('/entrar');
    });
    it('remove a página protegida quando a sessão é terminada', async () => {
      iniciar();
      const f = TestBed.createComponent(App);
      f.detectChanges();
      TestBed.inject(HttpTestingController)
        .expectOne(base + '/autenticacao/me')
        .flush({ id: 'aluno-1' });
      await TestBed.inject(Router).navigateByUrl('/inicio');
      await f.whenStable();
      TestBed.inject(SessaoService).terminar();
      f.detectChanges();
      await f.whenStable();
      expect(TestBed.inject(Router).url).toBe('/entrar');
    });
  });
});
