import { Component } from '@angular/core';

@Component({
  selector: 'app-home',
  template: `
  <div class="portal-home">
    <section class="portal-hero">
      <div class="portal-hero-copy">
        <span class="portal-eyebrow"></span>
        <h1>Una bienvenida<br>extraordinaria comienza aquí.</h1>
        <p>Registro digital, firma segura y atención personalizada en una sola experiencia conectada con OPERA Cloud.</p>
        <div class="portal-trust"><span><b>✓</b> Datos protegidos</span><span><b>✓</b> Proceso digital</span><span><b>✓</b> Atención ágil</span></div>
      </div>
    </section>
    <section class="portal-actions" aria-labelledby="portal-title">
      <div class="portal-actions-heading">
        <span class="guest-eyebrow">BIENVENIDO</span>
        <h2 id="portal-title">¿Cómo desea continuar?</h2>
        <p>Seleccione el acceso correspondiente para comenzar.</p>
      </div>
      <div class="panel text-center"><strong>Acceso operativo</strong><p class="mb-0 mt-2">Continúe desde las opciones autorizadas del menú.</p></div>
      <p class="portal-help">Si necesita asistencia, acérquese con uno de nuestros anfitriones.</p>
    </section>
  </div>`
})
export class HomeComponent {}
