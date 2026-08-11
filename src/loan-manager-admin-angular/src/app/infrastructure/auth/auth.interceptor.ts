import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { AuthService } from '../../application/auth/auth.service';

/**
 * Attaches the current session's bearer token to every outgoing request,
 * and on a 401 response (token missing/expired/rejected by the API),
 * clears the session and redirects to /login rather than leaving the user
 * looking at a broken screen.
 *
 * Only attaches the token for requests to our own API — not strictly
 * necessary here (nothing else is called), but the check is cheap
 * insurance against ever leaking this token to a third-party request
 * added later.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const token = authService.getToken();
  const isApiRequest = req.url.includes('/api/');

  const authorizedReq = token && isApiRequest
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authorizedReq).pipe(
    catchError((error) => {
      if (error.status === 401) {
        authService.logout();
        router.navigate(['/login']);
      }
      return throwError(() => error);
    }),
  );
};
