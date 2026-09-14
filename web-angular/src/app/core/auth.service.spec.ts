import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors, withNoXsrfProtection } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from './auth.service';
import { authInterceptor } from './auth.interceptor';

describe('Session and ASP.NET antiforgery', () => {
  let auth: AuthService;
  let http: HttpClient;
  let requests: HttpTestingController;
  const session = { username: 'test', displayName: 'Test', role: 'Admin', permissions: [] };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [
      provideRouter([]),
      provideHttpClient(withNoXsrfProtection(), withInterceptors([authInterceptor])),
      provideHttpClientTesting()
    ] });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpClient);
    requests = TestBed.inject(HttpTestingController);
  });
  afterEach(() => requests.verify());

  function csrf(token: string) {
    requests.expectOne('/api/auth/csrf').flush({ headerName: 'X-XSRF-TOKEN', token });
  }

  async function restore() {
    const result = firstValueFrom(auth.ensureSession());
    requests.expectOne('/api/auth/session').flush(session);
    csrf('authenticated-token');
    expect(await result).toBe(true);
  }

  it('renews the anonymous token after login and sends the authenticated token on writes', async () => {
    const result = firstValueFrom(auth.login('test', 'password'));
    csrf('anonymous-token');
    const login = requests.expectOne('/api/auth/login');
    expect(login.request.headers.get('X-XSRF-TOKEN')).toBe('anonymous-token');
    expect(login.request.withCredentials).toBe(true);
    login.flush(session);
    expect(auth.isAuthenticated).toBe(false);
    csrf('authenticated-token');
    expect(await result).toEqual(session);
    const save = firstValueFrom(http.post('/api/test', {}));
    const request = requests.expectOne('/api/test');
    expect(request.request.headers.get('X-XSRF-TOKEN')).toBe('authenticated-token');
    request.flush({});
    await save;
  });

  it('restores the request token when reloading a signed-in session', async () => {
    await restore();
    expect(auth.csrfToken).toBe('authenticated-token');
    expect(await firstValueFrom(auth.ensureSession())).toBe(true);
    requests.expectNone('/api/auth/session');
  });

  it('allows retrying session discovery after a server failure', async () => {
    const result = firstValueFrom(auth.ensureSession());
    requests.expectOne('/api/auth/session').flush({}, { status: 503, statusText: 'Unavailable' });
    expect(await result).toBe(false);
    expect(auth.sessionError).toContain('No se pudo comprobar');
    await restore();
    expect(auth.sessionError).toBe('');
  });

  it('treats an anonymous 401 as a normal signed-out session', async () => {
    const result = firstValueFrom(auth.ensureSession());
    requests.expectOne('/api/auth/session').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(await result).toBe(false);
    expect(auth.sessionError).toBe('');
    requests.expectNone('/api/auth/csrf');
  });

  it('shares CSRF discovery for concurrent writes and never sends a write without a token', async () => {
    const first = firstValueFrom(http.post('/api/first', {}));
    const second = firstValueFrom(http.post('/api/second', {}));
    requests.expectNone('/api/first');
    requests.expectNone('/api/second');
    csrf('shared-token');
    for (const url of ['/api/first', '/api/second']) {
      const request = requests.expectOne(url);
      expect(request.request.headers.get('X-XSRF-TOKEN')).toBe('shared-token');
      request.flush({});
    }
    await Promise.all([first, second]);
  });

  it('does not send a mutation when token discovery fails', async () => {
    const result = firstValueFrom(http.post('/api/test', {}));
    const failure = expect(result).rejects.toMatchObject({ status: 503 });
    requests.expectOne('/api/auth/csrf').flush({}, { status: 503, statusText: 'Unavailable' });
    await failure;
    requests.expectNone('/api/test');
  });

  it('preserves the local session when logout fails on the server', async () => {
    await restore();
    const result = firstValueFrom(auth.logout());
    const failure = expect(result).rejects.toMatchObject({ status: 503 });
    csrf('logout-token');
    requests.expectOne('/api/auth/logout').flush({}, { status: 503, statusText: 'Unavailable' });
    await failure;
    expect(auth.isAuthenticated).toBe(true);
  });

  it('clears the session and request token after a successful logout', async () => {
    await restore();
    const result = firstValueFrom(auth.logout());
    csrf('logout-token');
    requests.expectOne('/api/auth/logout').flush(null);
    await result;
    expect(auth.isAuthenticated).toBe(false);
    expect(auth.csrfToken).toBe('');
  });

  it('does not attach API credentials or antiforgery headers to external requests', async () => {
    auth.csrfToken = 'private-token';
    const result = firstValueFrom(http.post('https://example.invalid/test', {}));
    const request = requests.expectOne('https://example.invalid/test');
    expect(request.request.withCredentials).toBe(false);
    expect(request.request.headers.has('X-XSRF-TOKEN')).toBe(false);
    request.flush({});
    await result;
  });
});
