import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { ShellComponent } from './layout/shell.component';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/login/login.component').then(m => m.LoginComponent) },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'consultants', loadComponent: () => import('./features/consultants/consultants.component').then(m => m.ConsultantsComponent) },
      { path: 'clients', loadComponent: () => import('./features/clients/clients.component').then(m => m.ClientsComponent) },
      { path: 'demands', loadComponent: () => import('./features/demands/demands.component').then(m => m.DemandsComponent) },
      { path: 'demands/:id/matches', loadComponent: () => import('./features/match/match.component').then(m => m.MatchComponent) },
      { path: 'allocations', loadComponent: () => import('./features/allocations/allocations.component').then(m => m.AllocationsComponent) },
    ]
  },
  { path: '**', redirectTo: '' }
];
