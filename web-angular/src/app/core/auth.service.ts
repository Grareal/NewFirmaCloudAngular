import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, finalize, map, Observable, of, shareReplay, switchMap, tap, throwError, timeout } from 'rxjs';
import { AuthSession } from './models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private initialized = false;
  private sessionRequest?: Observable<boolean>;
  private csrfRequest?: Observable<string>;
  sessionError = '';

  username = '';
  displayName = '';
  role = '';
  permissions: string[] = [];
  csrfToken = '';

  get isAuthenticated() { return !!this.username; }
  hasPermission(permission: string) { return this.permissions.includes(permission); }

  ensureSession(): Observable<boolean> {
    if (this.initialized) return of(this.isAuthenticated);
    if (this.sessionRequest) return this.sessionRequest;
    this.sessionError = '';
    this.sessionRequest = this.http.get<AuthSession>('/api/auth/session').pipe(
      timeout(15000),
      switchMap(session => this.refreshCsrf().pipe(map(() => session))),
      tap(session => this.setSession(session)),
      map(() => true),
      catchError(error => {
        this.clear();
        if (error?.status !== 401) {
          this.initialized = false;
          this.sessionError = 'No se pudo comprobar la sesión. Verifica la conexión con el servidor e intenta nuevamente.';
        }
        return of(false);
      }),
      finalize(() => this.sessionRequest = undefined),
      shareReplay({ bufferSize: 1, refCount: true })
    );
    return this.sessionRequest;
  }

  login(username: string, password: string) {
    this.sessionError = '';
    return this.refreshCsrf().pipe(
      switchMap(() => this.http.post<AuthSession>('/api/auth/login', { username, password }).pipe(timeout(15000))),
      // The identity changed: the anonymous RequestToken is no longer valid.
      switchMap(session => this.refreshCsrf().pipe(map(() => session))),
      tap(session => this.setSession(session))
    );
  }

  logout() {
    return this.refreshCsrf().pipe(
      switchMap(() => this.http.post<void>('/api/auth/logout', {}).pipe(timeout(15000))),
      catchError(error => error?.status === 401 ? of(undefined) : throwError(() => error)),
      tap(() => this.clear())
    );
  }

  clear() {
    this.initialized = true;
    this.username = '';
    this.displayName = '';
    this.role = '';
    this.permissions = [];
    this.csrfToken = '';
    this.sessionError = '';
  }

  ensureCsrf(): Observable<string> {
    return this.csrfToken ? of(this.csrfToken) : this.refreshCsrf();
  }

  private refreshCsrf() {
    if (this.csrfRequest) return this.csrfRequest;
    this.csrfToken = '';
    this.csrfRequest = this.http.get<{ headerName: string; token: string }>('/api/auth/csrf').pipe(
      timeout(15000),
      map(response => {
        if (!response.token) throw new Error('El servidor no devolvió el token de sesión.');
        return response.token;
      }),
      tap(token => {
        this.csrfToken = token;
        this.csrfRequest = undefined;
      }),
      finalize(() => this.csrfRequest = undefined),
      shareReplay({ bufferSize: 1, refCount: true })
    );
    return this.csrfRequest;
  }

  private setSession(session: AuthSession) {
    this.initialized = true;
    this.username = session.username || '';
    this.displayName = session.displayName || '';
    this.role = session.role || '';
    this.permissions = session.permissions || [];
  }
}
