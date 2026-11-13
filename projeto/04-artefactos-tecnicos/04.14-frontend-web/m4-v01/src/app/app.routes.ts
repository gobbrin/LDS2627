import { Routes } from '@angular/router';
import { autenticadoGuard, visitanteGuard } from './core/guards';
export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [visitanteGuard],
    loadComponent: () => import('./features/inicio/inicio.page').then((m) => m.InicioPage),
    title: 'Campus Carpool',
  },
  {
    path: 'entrar',
    canActivate: [visitanteGuard],
    loadComponent: () => import('./features/autenticacao/login.page').then((m) => m.LoginPage),
    title: 'Entrar · Campus Carpool',
  },
  {
    path: 'registo',
    canActivate: [visitanteGuard],
    loadComponent: () => import('./features/autenticacao/registo.page').then((m) => m.RegistoPage),
    title: 'Criar conta · Campus Carpool',
  },
  {
    // Sem guarda: a ligação do email tem de funcionar mesmo com uma sessão aberta neste browser.
    path: 'confirmar-conta',
    loadComponent: () =>
      import('./features/autenticacao/confirmacao.page').then((m) => m.ConfirmacaoPage),
    title: 'Confirmar conta · Campus Carpool',
  },
  {
    path: 'reenviar-confirmacao',
    canActivate: [visitanteGuard],
    loadComponent: () =>
      import('./features/autenticacao/reenvio-confirmacao.page').then(
        (m) => m.ReenvioConfirmacaoPage,
      ),
    title: 'Nova ligação de confirmação · Campus Carpool',
  },
  {
    path: 'inicio',
    canActivate: [autenticadoGuard],
    loadComponent: () => import('./features/inicio/inicio.page').then((m) => m.InicioPage),
    title: 'Início · Campus Carpool',
  },
  {
    path: 'perfil',
    canActivate: [autenticadoGuard],
    loadComponent: () => import('./features/perfil/perfil.page').then((m) => m.PerfilPage),
    title: 'Perfil · Campus Carpool',
  },
  {
    path: 'viagens',
    canActivate: [autenticadoGuard],
    loadComponent: () =>
      import('./features/viagens/minhas-viagens.page').then((m) => m.MinhasViagensPage),
    title: 'As minhas viagens · Campus Carpool',
  },
  {
    path: 'viagens/nova',
    canActivate: [autenticadoGuard],
    loadComponent: () =>
      import('./features/viagens/criar-viagem.page').then((m) => m.CriarViagemPage),
    title: 'Criar viagem · Campus Carpool',
  },
  {
    path: 'viagens/:id/editar',
    canActivate: [autenticadoGuard],
    loadComponent: () =>
      import('./features/viagens/editar-viagem.page').then((m) => m.EditarViagemPage),
    title: 'Gerir viagem · Campus Carpool',
  },
  {
    // Fora de /perfil para o menu não assinalar «Perfil» ao consultar outra pessoa.
    path: 'utilizadores/:id',
    canActivate: [autenticadoGuard],
    loadComponent: () =>
      import('./features/perfil/perfil-publico.page').then((m) => m.PerfilPublicoPage),
    title: 'Perfil de utilizador · Campus Carpool',
  },
  {
    path: '**',
    loadComponent: () => import('./shared/nao-encontrada.page').then((m) => m.NaoEncontradaPage),
    title: 'Página não encontrada · Campus Carpool',
  },
];
