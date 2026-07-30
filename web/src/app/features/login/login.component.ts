import { Component, NgZone, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressBarModule,
  ],
  template: `
    <div class="wrap">
      <mat-card class="card">
        @if (loading()) { <mat-progress-bar mode="indeterminate"></mat-progress-bar> }
        <mat-card-header>
          <mat-card-title><mat-icon>hub</mat-icon> AllocationHub</mat-card-title>
          <mat-card-subtitle>Staffing & allocation console&nbsp; <span class="lv-badge">for Lyncas</span></mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <form [formGroup]="form" (ngSubmit)="submit()">
            <mat-form-field appearance="outline" class="full">
              <mat-label>Email</mat-label>
              <input matInput type="email" formControlName="email" autocomplete="username" />
            </mat-form-field>
            <mat-form-field appearance="outline" class="full">
              <mat-label>Password</mat-label>
              <input matInput type="password" formControlName="password" autocomplete="current-password" />
            </mat-form-field>
            @if (error()) { <p class="error">{{ error() }}</p> }
            <button mat-flat-button color="primary" class="full" [disabled]="form.invalid || loading()">Sign in</button>
          </form>
          @if (googleEnabled()) {
            <div class="divider"><span>or</span></div>
            <div id="googleBtn" class="gbtn"></div>
          }
          <p class="hint">Demo: admin&#64;demo.com / admin123</p>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .wrap { min-height: 100vh; display: grid; place-items: center; background: var(--lv-primary-wash); }
    .card { width: 360px; max-width: 92vw; border-top: 4px solid var(--lv-primary); }
    .full { width: 100%; }
    mat-card-title { display: flex; align-items: center; gap: 6px; color: var(--lv-primary-dark); }
    .error { color: var(--lv-err); margin: 0 0 8px; font-size: .9rem; }
    .hint { text-align: center; color: #777; font-size: .8rem; margin: 8px 0 0; }
    .divider { display: flex; align-items: center; gap: 10px; color: #999; font-size: .8rem; margin: 14px 0 10px; }
    .divider::before, .divider::after { content: ''; flex: 1; border-top: 1px solid #e0e0e0; }
    .gbtn { display: flex; justify-content: center; min-height: 40px; }
  `],
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private zone = inject(NgZone);

  loading = signal(false);
  error = signal<string | null>(null);
  googleEnabled = signal(false);
  private googleClientId = '';

  form = this.fb.nonNullable.group({
    email: ['admin@demo.com', [Validators.required, Validators.email]],
    password: ['admin123', Validators.required],
  });

  constructor() {
    // Google button only appears when the API says it's configured — graceful degradation,
    // password login is always the fallback.
    this.auth.authConfig().subscribe({
      next: c => {
        if (!c.googleEnabled || !c.googleClientId) return;
        this.googleClientId = c.googleClientId;
        this.googleEnabled.set(true);
        this.loadGoogleScript();
      },
      error: () => { /* API down: the password form still renders */ },
    });
  }

  private loadGoogleScript(): void {
    if ((window as any).google?.accounts?.id) { this.renderGoogleButton(); return; }
    const s = document.createElement('script');
    s.src = 'https://accounts.google.com/gsi/client';
    s.async = true;
    s.onload = () => this.renderGoogleButton();
    document.head.appendChild(s);
  }

  private renderGoogleButton(): void {
    // The @if needs a tick to put #googleBtn in the DOM.
    setTimeout(() => {
      const g = (window as any).google;
      const host = document.getElementById('googleBtn');
      if (!g?.accounts?.id || !host) return;
      g.accounts.id.initialize({
        client_id: this.googleClientId,
        // GIS calls back outside Angular — re-enter the zone so signals/router work.
        callback: (resp: { credential: string }) => this.zone.run(() => this.onGoogleCredential(resp.credential)),
      });
      g.accounts.id.renderButton(host, { theme: 'outline', size: 'large', width: 300 });
    });
  }

  private onGoogleCredential(idToken: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.auth.googleLogin(idToken).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: err => {
        this.error.set(err?.error?.message ?? 'Google sign-in failed.');
        this.loading.set(false);
      },
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);
    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: () => { this.error.set('Invalid email or password.'); this.loading.set(false); },
    });
  }
}
