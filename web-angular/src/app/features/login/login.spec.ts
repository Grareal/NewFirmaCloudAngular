import { NgZone, provideZoneChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { LoginComponent } from './login';

describe('Login errors', () => {
  it.each([
    [401, 'Usuario o contraseña inválidos.'],
    [503, 'error del servidor'],
    [0, 'No hay conexión con el servidor'],
    [400, 'No se pudo validar la solicitud']
  ])('shows the correct asynchronous error for HTTP %s and releases the button', async (status, message) => {
    TestBed.configureTestingModule({ providers: [
      provideRouter([]), provideZoneChangeDetection(),
      { provide: AuthService, useValue: {
        sessionError: '',
        login: () => new Observable(subscriber => {
          const timer = setTimeout(() => subscriber.error({ status }), 10);
          return () => clearTimeout(timer);
        })
      } }
    ] });
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.autoDetectChanges();
    fixture.componentInstance.username = 'test';
    fixture.componentInstance.password = 'password';
    TestBed.inject(NgZone).run(() => fixture.componentInstance.submit());
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain(message);
    expect(fixture.nativeElement.querySelector('button').disabled).toBe(false);
    expect(fixture.nativeElement.textContent).not.toContain('Verificando');
  });
});
