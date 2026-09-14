import { NgZone, provideZoneChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SearchComponent } from './search';
import { ApiService } from '../../core/api.service';

describe('Search rendering', () => {
  it('updates the result and releases the button after an asynchronous response without another click', async () => {
    TestBed.configureTestingModule({ providers: [
      provideRouter([]), provideZoneChangeDetection(),
      { provide: ApiService, useValue: {
        searchReservations: () => new Promise(resolve => setTimeout(() => resolve([]), 10))
      } }
    ] });
    const fixture = TestBed.createComponent(SearchComponent);
    fixture.autoDetectChanges();
    fixture.componentInstance.term = 'test';
    TestBed.inject(NgZone).run(() => fixture.componentInstance.search());
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Sin coincidencias');
    expect(fixture.nativeElement.querySelector('button[type="submit"]').disabled).toBe(false);
    expect(fixture.nativeElement.textContent).not.toContain('Consultando OPERA Cloud');
  });
});
