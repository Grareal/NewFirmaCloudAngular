import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/')) return next(request);
  const auth = inject(AuthService);
  const router = inject(Router);
  const authenticatedRequest = request.clone({ withCredentials: true });
  const changesData = !['GET', 'HEAD', 'OPTIONS'].includes(request.method);
  const response = changesData
    ? auth.ensureCsrf().pipe(switchMap(token => next(authenticatedRequest.clone({
        setHeaders: { 'X-XSRF-TOKEN': token }
      }))))
    : next(authenticatedRequest);
  return response.pipe(catchError(error => {
    const isAuthenticationCall = request.url.includes('/api/auth/');
    if (error?.status === 401 && !isAuthenticationCall) {
      auth.clear();
      void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
    }
    return throwError(() => error);
  }));
};
