import { Component, inject, signal } from '@angular/core';
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
          <mat-card-subtitle>Staffing & allocation console</mat-card-subtitle>
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
          <p class="hint">Demo: admin&#64;demo.com / admin123</p>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .wrap { min-height: 100vh; display: grid; place-items: center; background: #eef1f6; }
    .card { width: 360px; max-width: 92vw; }
    .full { width: 100%; }
    mat-card-title { display: flex; align-items: center; gap: 6px; }
    .error { color: #c62828; margin: 0 0 8px; font-size: .9rem; }
    .hint { text-align: center; color: #777; font-size: .8rem; margin: 8px 0 0; }
  `],
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  loading = signal(false);
  error = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    email: ['admin@demo.com', [Validators.required, Validators.email]],
    password: ['admin123', Validators.required],
  });

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
