import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.ensureSession().pipe(map(authenticated => {
    if (!authenticated) {
      return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
    }

    const permission = route.data?.['permission'] as string | undefined;
    if (permission && !auth.hasPermission(permission) && auth.role !== 'Admin') {
      return router.createUrlTree(['/']);
    }
    return true;
  }));
};
