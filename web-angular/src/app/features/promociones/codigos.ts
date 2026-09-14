import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { UdfCode } from '../../core/models';

@Component({ selector: 'app-codigos', imports: [FormsModule], templateUrl: './codigos.html' })
export class CodigosComponent implements OnInit {
  private readonly api = inject(ApiService);
  rows: UdfCode[] = []; filtered: UdfCode[] = [];
  query = ''; field = 'all'; page = 1; pageSize = 50;
  loading = false; error = ''; loaded = false; updatedAt = '';
  ngOnInit() { this.refresh(); }
  refresh() {
    if (this.loading) return;
    this.loading = true; this.error = '';
    this.api.getUdfCodes().then(rows => {
      this.rows = rows; this.loaded = true; this.updatedAt = new Date().toLocaleString('es-MX');
      this.applyFilter(); this.loading = false;
    }, error => {
      this.error = error?.error?.message || 'No se pudo consultar el catálogo en OPERA. Intenta nuevamente.';
      this.loading = false;
    });
  }
  applyFilter() {
    const fragments = this.normalize(this.query).split(/\s+/).filter(Boolean);
    this.filtered = this.rows.filter(row => {
      const text = this.normalize(this.field === 'code' ? row.code : this.field === 'description' ? row.description : row.code + ' ' + row.description);
      return fragments.every(fragment => text.includes(fragment));
    });
    this.page = 1;
  }
  clear() { this.query = ''; this.field = 'all'; this.applyFilter(); }
  get pages() { return Math.max(1, Math.ceil(this.filtered.length / this.pageSize)); }
  get visible() { return this.filtered.slice((this.page - 1) * this.pageSize, this.page * this.pageSize); }
  get first() { return this.filtered.length ? (this.page - 1) * this.pageSize + 1 : 0; }
  get last() { return Math.min(this.page * this.pageSize, this.filtered.length); }
  private normalize(value: string) { return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim(); }
}
