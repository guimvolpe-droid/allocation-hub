import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive,
    MatToolbarModule, MatSidenavModule, MatListModule, MatIconModule, MatButtonModule,
  ],
  template: `
    <mat-toolbar color="primary" class="topbar">
      <span class="brand"><mat-icon>hub</mat-icon>&nbsp;AllocationHub</span>
      <span class="spacer"></span>
      <span class="who">{{ auth.user()?.name }}</span>
      <button mat-icon-button (click)="auth.logout()" title="Sign out"><mat-icon>logout</mat-icon></button>
    </mat-toolbar>

    <mat-sidenav-container class="container">
      <mat-sidenav mode="side" opened class="sidenav">
        <mat-nav-list>
          <a mat-list-item routerLink="/dashboard" routerLinkActive="active"><mat-icon matListItemIcon>dashboard</mat-icon> Dashboard</a>
          <a mat-list-item routerLink="/consultants" routerLinkActive="active"><mat-icon matListItemIcon>groups</mat-icon> Consultants</a>
          <a mat-list-item routerLink="/clients" routerLinkActive="active"><mat-icon matListItemIcon>business</mat-icon> Clients</a>
          <a mat-list-item routerLink="/demands" routerLinkActive="active"><mat-icon matListItemIcon>work</mat-icon> Demands</a>
          <a mat-list-item routerLink="/allocations" routerLinkActive="active"><mat-icon matListItemIcon>assignment_ind</mat-icon> Allocations</a>
          <div class="section">Admin</div>
          <a mat-list-item routerLink="/settings" routerLinkActive="active"><mat-icon matListItemIcon>tune</mat-icon> Matching settings</a>
          <a mat-list-item routerLink="/audit" routerLinkActive="active"><mat-icon matListItemIcon>history</mat-icon> Audit log</a>
        </mat-nav-list>
      </mat-sidenav>
      <mat-sidenav-content class="content">
        <router-outlet></router-outlet>
      </mat-sidenav-content>
    </mat-sidenav-container>
  `,
  styles: [`
    .topbar { position: sticky; top: 0; z-index: 10; }
    .brand { display: inline-flex; align-items: center; font-weight: 600; }
    .spacer { flex: 1 1 auto; }
    .who { margin-right: 8px; font-size: .9rem; opacity: .9; }
    .container { position: absolute; top: 64px; bottom: 0; left: 0; right: 0; }
    .sidenav { width: 232px; padding-top: 8px; border-right: 1px solid rgba(0,0,0,.08); }
    .content { padding: 24px; background: #f7f8fa; }
    a.active { background: rgba(0,0,0,.06); font-weight: 600; }
    .section { padding: 14px 16px 4px; font-size: .72rem; text-transform: uppercase; letter-spacing: .06em; color: #999; }
  `],
})
export class ShellComponent {
  auth = inject(AuthService);
}
