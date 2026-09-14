import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  template: `
  <div class="login-shell">
    <div class="login-brand">
      <div><span class="brand-mark">V</span></div>
      <h1>Firma Vidanta</h1>
      <p>Registro digital · OPERA Cloud</p>
    </div>
    <div class="login-form">
      <h2>Iniciar sesión</h2>
      @if (error || auth.sessionError) { <div class="alert alert-danger" role="alert">{{ error || auth.sessionError }}</div> }
      <form (ngSubmit)="submit()">
        <div class="mb-3"><label class="form-label">Usuario</label><input class="form-control" [(ngModel)]="username" name="u" autocomplete="username" required></div>
        <div class="mb-3"><label class="form-label">Contraseña</label><input class="form-control" type="password" [(ngModel)]="password" name="p" autocomplete="current-password" required></div>
        <button class="btn btn-primary w-100" [disabled]="busy">{{ busy ? 'Verificando…' : 'Entrar' }}</button>
      </form>
    </div>
  </div>`,
  styles: [`.login-form { max-width: 420px; margin: auto; padding: 2rem; }`]
})
export class LoginComponent {
  auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  username = ''; password = ''; busy = false; error = '';

  submit() {
    if (this.busy || !this.username.trim() || !this.password) return;
    this.busy = true; this.error = '';
    this.auth.login(this.username.trim(), this.password).pipe(
      finalize(() => this.busy = false)
    ).subscribe({
      next: () => {
        const ret = this.route.snapshot.queryParams['returnUrl'];
        void this.router.navigateByUrl(typeof ret === 'string' && ret.startsWith('/') && !ret.startsWith('//') && !ret.startsWith('/login') ? ret : '/');
      },
      error: error => {
        this.error = error?.status === 401 ? 'Usuario o contraseña inválidos.'
          : error?.status === 400 ? 'No se pudo validar la solicitud de acceso. Intenta nuevamente.'
          : error?.status === 0 || error?.name === 'TimeoutError' ? 'No hay conexión con el servidor. Verifica que la API esté disponible e intenta nuevamente.'
          : 'No se pudo iniciar sesión por un error del servidor. Intenta nuevamente.';
      }
    });
  }
}
