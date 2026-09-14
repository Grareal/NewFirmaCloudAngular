import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors, withNoXsrfProtection } from '@angular/common/http';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection(),
    provideRouter(routes),
    // ASP.NET requires its RequestToken, not the antiforgery cookie value.
    provideHttpClient(withNoXsrfProtection(), withInterceptors([authInterceptor])),
  ]
};
