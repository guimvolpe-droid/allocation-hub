import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { API_URL } from './config';
import { AuthConfig, AuthResponse, User } from './models';

const TOKEN_KEY = 'ah_token';
const USER_KEY = 'ah_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private _user = signal<User | null>(this.readUser());
  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);

  get token(): string | null { return localStorage.getItem(TOKEN_KEY); }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${API_URL}/auth/login`, { email, password })
      .pipe(tap(res => this.persist(res)));
  }

  /** Which sign-in methods the API offers (Google button only shows when configured). */
  authConfig(): Observable<AuthConfig> {
    return this.http.get<AuthConfig>(`${API_URL}/auth/config`);
  }

  /** OAuth: exchange the Google ID token for OUR JWT — same session shape as password login. */
  googleLogin(idToken: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${API_URL}/auth/google`, { idToken })
      .pipe(tap(res => this.persist(res)));
  }

  private persist(res: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, res.token);
    localStorage.setItem(USER_KEY, JSON.stringify(res.user));
    this._user.set(res.user);
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._user.set(null);
    this.router.navigate(['/login']);
  }

  private readUser(): User | null {
    const raw = localStorage.getItem(USER_KEY);
    return raw && localStorage.getItem(TOKEN_KEY) ? JSON.parse(raw) as User : null;
  }
}
