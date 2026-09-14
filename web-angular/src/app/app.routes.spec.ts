import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NavigationError, provideRouter, Router, Routes } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { Observable, of } from 'rxjs';
import { routes } from './app.routes';
import { AuthService } from './core/auth.service';

@Component({ template: 'Route matched' })
class RouteProbe {}

// Exercise the real URL patterns, ordering, redirects and guard without
// instantiating operational screens that would request backend data.
const routingOnly: Routes = routes.map(route => {
  const { loadComponent, ...config } = route;
  return loadComponent ? { ...config, component: RouteProbe } : config;
});

describe('Application route recognition', () => {
  let auth: { authenticated: boolean; role: string; ensureSession: () => Observable<boolean>; hasPermission: () => boolean };
  let failures: NavigationError[];

  beforeEach(() => {
    auth = {
      authenticated: true, role: 'Admin',
      ensureSession: () => of(auth.authenticated),
      hasPermission: () => false
    };
    failures = [];
    TestBed.configureTestingModule({ providers: [
      provideRouter(routingOnly), { provide: AuthService, useValue: auth }
    ] });
    TestBed.inject(Router).events.subscribe(event => {
      if (event instanceof NavigationError) failures.push(event);
    });
  });

  const urls = routes.filter(route => route.path !== '**').map(route =>
    '/' + route.path!.replace(':confirmation', '123456789')
  );

  it.each(urls)('recognizes %s without a navigation error', async url => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url, RouteProbe);
    expect(TestBed.inject(Router).url).toBe(url === '/udf' ? '/codigos-promocion' : url === '/busqueda' ? '/operacion' : url);
    expect(failures).toEqual([]);
  });

  it('preserves the complete destination when redirecting an anonymous user to login', async () => {
    auth.authenticated = false;
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/huesped/123456789?origin=search', RouteProbe);
    const router = TestBed.inject(Router);
    const tree = router.parseUrl(router.url);
    expect(tree.root.children['primary'].segments[0].path).toBe('login');
    expect(tree.queryParams['returnUrl']).toBe('/huesped/123456789?origin=search');
    expect(failures).toEqual([]);
  });

  it('redirects a user without permission to home without a redirect loop', async () => {
    auth.role = 'User';
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/usuarios', RouteProbe);
    expect(TestBed.inject(Router).url).toBe('/');
    expect(failures).toEqual([]);
  });

  it('handles an unknown URL through the wildcard instead of NG04002', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/unknown/nested/page', RouteProbe);
    const router = TestBed.inject(Router);
    expect(router.routerState.snapshot.root.firstChild?.routeConfig?.path).toBe('**');
    expect(failures).toEqual([]);
  });
});
