import { Component, ViewChild, inject, OnDestroy, OnInit } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { OfficialCardInput, OfficialCardOccupantInput, Reservation } from '../../core/models';
import { SignatureCanvasComponent } from '../../shared/signature-canvas';

const normalizedName = (value?: string) => (value ?? '')
  .normalize('NFD')
  .replace(/[\u0300-\u036f]/g, '')
  .replace(/\s+/g, ' ')
  .trim()
  .toLocaleUpperCase('es-MX');

/** Fusiona las dos representaciones solapadas de acompañantes que entrega OPERA. */
export function reconcileOccupants(
  reservation: Reservation,
  existing: OfficialCardOccupantInput[],
  createId: () => string = () => crypto.randomUUID()
): OfficialCardOccupantInput[] {
  const primaryName = normalizedName(reservation.guest.fullName);
  const primaryId = reservation.guest.id?.trim();
  const detailed = (reservation.accompanyingGuests ?? []).map(guest => ({
    name: guest.fullName?.replace(/\s+/g, ' ').trim(),
    signerId: (guest.profileId ?? guest.reservationGuestId)?.trim() || undefined
  }));
  const candidates = detailed.concat(
    (reservation.accompanyingGuestNames ?? []).map(name => ({
      name: name?.replace(/\s+/g, ' ').trim(),
      signerId: undefined
    }))
  );
  const result: OfficialCardOccupantInput[] = [];
  const seenIds = new Set<string>();
  const usedSavedIds = new Set<string>();
  const detailedNames = new Set(detailed.filter(item => item.signerId).map(item => normalizedName(item.name)));

  for (const candidate of candidates) {
    const nameKey = normalizedName(candidate.name);
    if (!nameKey || nameKey === primaryName || (candidate.signerId && candidate.signerId === primaryId)) continue;
    if (candidate.signerId) {
      if (seenIds.has(candidate.signerId)) continue;
      seenIds.add(candidate.signerId);
    } else if (detailedNames.has(nameKey) || result.some(item => !item.signerId && normalizedName(item.name) === nameKey)) {
      continue;
    }

    const available = existing.filter(item => !item.clientId || !usedSavedIds.has(item.clientId));
    const exactIdMatches = candidate.signerId ? available.filter(item => item.signerId === candidate.signerId) : [];
    const savedCandidates = exactIdMatches.length
      ? exactIdMatches
      : available.filter(item => normalizedName(item.name) === nameKey);
    const saved = savedCandidates.find(item => !!item.signaturePngBase64) ?? savedCandidates[0];
    if (saved?.clientId) usedSavedIds.add(saved.clientId);
    result.push({
      clientId: saved?.clientId || createId(),
      signerId: candidate.signerId,
      name: candidate.name,
      signaturePngBase64: saved?.signaturePngBase64 || '',
      selected: true
    });
    if (result.length === 8) break;
  }

  return result;
}

@Component({
  selector: 'app-tarjeta',
  imports: [FormsModule, RouterLink, SignatureCanvasComponent],
  templateUrl: './tarjeta.html',
  styleUrl: './tarjeta.css'

})
export class TarjetaComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  private sanitizer = inject(DomSanitizer);
  @ViewChild('pad') pad?: SignatureCanvasComponent;
  confirmation = '';
  steps = [{ n: 1, label: 'Datos' }, { n: 2, label: 'Firmas' }, { n: 3, label: 'Revisión' }];
  step = 1; activeSigner: number | null = null;
  revision: string | null = null; savedAt = ''; draftUnavailable = false;
  reservation: Reservation | null = null;
  input: OfficialCardInput = { marketingConsent: false, signatureAuthorizationAccepted: false, occupants: [] };
  loading = true; working = false; previewReady = false; confirmSend = false;
  message = ''; isError = false;
  previewBlob: Blob | null = null; previewUrl: SafeResourceUrl | null = null;
  private previewObjectUrl: string | null = null;

  ngOnInit() {
    this.confirmation = this.route.snapshot.params['confirmation'];
    Promise.all([this.api.getReservation(this.confirmation), this.api.getSignatureDraft(this.confirmation)]).then(
      ([list, draft]) => {
        const r = list[0];
        if (r) {
          this.reservation = r;
          this.input.primaryGuestName = r.guest.fullName;
          this.input.primarySignerId = r.guest.id;
          this.input.email = r.guest.email; this.input.cellPhone = r.guest.phoneNumber;
          this.input.city = r.guest.address?.city; this.input.state = r.guest.address?.stateProvCode; this.input.country = r.guest.address?.countryCode;
        }
        if (draft.input) this.input = draft.input;
        this.input.signatureAuthorizationAccepted ??= false;
        if (r) this.syncOccupantsFromOpera(r);
        this.revision = draft.revision;
        this.savedAt = draft.updatedAtUtc ? new Date(draft.updatedAtUtc).toLocaleString('es-MX') : '';
        this.loading = false;
      },
      e => { this.draftUnavailable = true; this.setMsg(e?.error?.message || 'No se pudo recuperar la reserva y sus firmas guardadas. Recarga para reintentar.', true); this.loading = false; }
    );
  }

  ngOnDestroy() { this.clearPreview(); }

  private syncOccupantsFromOpera(reservation: Reservation) {
    this.input.occupants = reconcileOccupants(reservation, this.input.occupants || []);
  }

  get totalSigners() { return this.input.occupants.length + 1; }
  get signedCount() { return Number(!!this.input.primarySignaturePngBase64) + this.input.occupants.filter(o => !!o.signaturePngBase64).length; }
  get currentSigner() { return this.activeSigner === -1 ? this.input.primaryGuestName : this.input.occupants[this.activeSigner ?? -1]?.name; }
  invalidatePreview() { this.previewReady = false; this.confirmSend = false; this.clearPreview(); }

  startSignatures() {
    this.step = 2; this.activeSigner = null; this.invalidatePreview(); this.msg('');
  }
  selectSigner(i: number) {
    if (this.working || (i === -1 ? !!this.input.primarySignaturePngBase64 : !!this.input.occupants[i]?.signaturePngBase64)) return;
    this.pad?.clear(); this.activeSigner = i;
  }
  capture() {
    if (this.activeSigner === null || this.working) return;
    if (!this.input.signatureAuthorizationAccepted) { this.setMsg('Debe verificar los datos y autorizar el uso de las firmas antes de capturarlas.', true); return; }
    const png = this.pad?.getPng();
    if (!png) { this.setMsg('Capture la firma antes de confirmar.', true); return; }
    if (this.activeSigner === -1) this.input.primarySignaturePngBase64 = png;
    else this.input.occupants[this.activeSigner].signaturePngBase64 = png;
    this.activeSigner = null; this.invalidatePreview();
    this.saveProgress();
  }
  saveProgress() {
    if (this.working || this.draftUnavailable) return;
    this.working = true;
    this.persist().then(
      () => { this.msg('Avance guardado. Puedes regresar después para capturar las firmas pendientes.'); this.working = false; },
      e => { this.setMsg(e?.error?.message || 'No se guardó el avance. No cierres esta página; reintenta Guardar avance.', true); this.working = false; }
    );
  }
  private persist() {
    this.input.occupants.forEach(o => o.clientId ||= crypto.randomUUID());
    return this.api.saveSignatureDraft(this.confirmation, this.revision, structuredClone(this.input)).then(draft => {
      this.revision = draft.revision;
      this.savedAt = draft.updatedAtUtc ? new Date(draft.updatedAtUtc).toLocaleString('es-MX') : '';
    });
  }
  preview() {
    this.working = true; this.msg('');
    this.persist().then(() => this.api.previewOfficialCard(this.confirmation, this.input)).then(
      b => {
        this.clearPreview();
        this.previewBlob = b;
        this.previewObjectUrl = URL.createObjectURL(b);
        this.previewUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.previewObjectUrl);
        this.previewReady = true;
        this.setMsg('Vista previa generada. Revísala en esta pantalla antes de enviar.', false);
        this.working = false;
      },
      e => { this.setMsg(e?.error?.message || e.message, true); this.working = false; }
    );
  }
  downloadPreview() { if (this.previewBlob) this.api.downloadBlob(this.previewBlob, `REGCARD${this.confirmation}PREVIEW.pdf`); }
  private clearPreview() {
    if (this.previewObjectUrl) URL.revokeObjectURL(this.previewObjectUrl);
    this.previewObjectUrl = null;
    this.previewUrl = null;
    this.previewBlob = null;
  }
  upload() {
    this.working = true; this.msg('');
    this.persist().then(() => this.api.uploadOfficialCard(this.confirmation, this.input)).then(
      r => {
        const mail = r.emailStatus === 'Pending' ? ' El correo quedó en cola para envío SMTP.' : r.emailStatus === 'SkippedNoEmail' ? ' No se encoló correo: sin destinatario válido.' : r.emailStatus === 'QueueFailed' ? ' No fue posible encolar el correo.' : '';
        const opera = r.attachmentOutcome === 'Replaced'
          ? `La versión ${r.localDocumentVersion} reemplazó el adjunto existente en OPERA.`
          : r.attachmentOutcome === 'SkippedExisting'
            ? `OPERA ya contenía ${r.fileName}; la política ${r.attachmentPolicy} conservó el adjunto existente y no subió esta versión.`
            : `Versión ${r.localDocumentVersion} enviada a OPERA como ${r.fileName}.`;
        this.setMsg(`${opera} Attachment ID: ${r.attachmentId}.${mail}`, r.emailStatus === 'QueueFailed');
        this.confirmSend = false; this.working = false;
      },
      e => { this.setMsg(typeof e?.error === 'string' ? e.error : e.message, true); this.working = false; }
    );
  }
  dash(v?: string) { return v?.trim() ? v : '—'; }
  private msg(t: string) { this.message = t; this.isError = false; }
  private setMsg(t: string, err: boolean) { this.message = t; this.isError = err; }
}
