import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
import { ApiService } from './core/api.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  auth = inject(AuthService);
  private router = inject(Router);
  private api = inject(ApiService);
  env = '';
  connectionError = '';
  logoutError = '';

  constructor() {
    this.loadEnvironment();
  }

  loadEnvironment() {
    this.connectionError = '';
    this.api.getEnvironment().then(
      info => this.env = info.isUat ? 'UAT' : info.environment,
      () => this.connectionError = 'No se pudo conectar con el servidor.'
    );
  }

  has(p: string) { return this.auth.hasPermission(p) || this.auth.role === 'Admin'; }
  get initial() { return (this.auth.displayName || 'U').slice(0, 1).toUpperCase(); }
  logout() {
    this.logoutError = '';
    this.auth.logout().subscribe({
      next: () => void this.router.navigate(['/login']),
      error: () => this.logoutError = 'No se pudo cerrar la sesión en el servidor. Intenta nuevamente.'
    });
  }
  get showNav() {
    let route = this.router.routerState.snapshot.root;
    while (route.firstChild) route = route.firstChild;
    return this.auth.isAuthenticated && !this.router.url.startsWith('/login') && route.data['layout'] !== 'guest';
  }
  get isUat() { return this.env === 'UAT'; }
}
