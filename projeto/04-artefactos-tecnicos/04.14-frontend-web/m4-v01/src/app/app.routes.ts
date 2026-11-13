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
    path: '**',
    loadComponent: () => import('./shared/nao-encontrada.page').then((m) => m.NaoEncontradaPage),
    title: 'Página não encontrada · Campus Carpool',
  },
];
