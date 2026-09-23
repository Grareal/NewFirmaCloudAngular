import { Component, ViewChild, inject, OnDestroy, OnInit } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { OfficialCardInput, OfficialCardOccupantInput, Reservation, SignatureDraft } from '../../core/models';
import { SignatureCanvasComponent } from '../../shared/signature-canvas';

@Component({
  selector: 'app-tarjeta',
  imports: [FormsModule, RouterLink, SignatureCanvasComponent],
  templateUrl: './tarjeta.html'

})
export class TarjetaComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  private sanitizer = inject(DomSanitizer);
  @ViewChild('pad') pad?: SignatureCanvasComponent;
  confirmation = '';
  steps = [{ n: 1, label: 'Datos' }, { n: 2, label: 'Ocupantes' }, { n: 3, label: 'Firmas' }, { n: 4, label: 'Revisión' }];
  step = 1; activeSigner: number | null = null; primarySelected = true;
  revision: string | null = null; savedAt = ''; draftUnavailable = false;
  reservation: Reservation | null = null;
  input: OfficialCardInput = { marketingConsent: false, signatureAuthorizationAccepted: false, occupants: [] };
  loading = true; working = false; previewReady = false; confirmSend = false;
  message = ''; isError = false;
  operaGiven = ''; operaSurname = ''; operaWorking = false; confirmOpera = false;
  lookup: any = null; guestPreview: any = null; operaProfileId = '';
  previewBlob: Blob | null = null; previewUrl: SafeResourceUrl | null = null;
  private previewObjectUrl: string | null = null;
  // Se conserva el código de edición para reactivarlo únicamente si Operación lo aprueba.
  readonly allowCompanionEditing = false;

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
          for (const g of (r.accompanyingGuests || []).slice(0, 8))
            this.input.occupants.push({ name: g.fullName, signerId: g.profileId ?? g.reservationGuestId, signaturePngBase64: '', selected: true, clientId: crypto.randomUUID() });
          if (!this.input.occupants.length)
            for (const n of (r.accompanyingGuestNames || []).slice(0, 8))
              this.input.occupants.push({ name: n, signaturePngBase64: '', selected: true });
          const expected = Math.min(8, Math.max(0, r.roomStay.adultCount - 1));
          while (this.input.occupants.length < expected)
            this.input.occupants.push({ name: '', signaturePngBase64: '', selected: true });
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
    const existing = this.input.occupants || [];
    const opera = (reservation.accompanyingGuests || []).map(g => ({ name: g.fullName, signerId: g.profileId ?? g.reservationGuestId }))
      .concat((reservation.accompanyingGuestNames || []).map(name => ({ name, signerId: undefined })))
      .filter((g, index, all) => !!g.name?.trim() && all.findIndex(x => (x.signerId && x.signerId === g.signerId) || (!x.signerId && x.name.trim().toUpperCase() === g.name.trim().toUpperCase())) === index)
      .slice(0, 8);
    this.input.occupants = opera.map(g => {
      const saved = existing.find(x => (g.signerId && x.signerId === g.signerId) || x.name.trim().toUpperCase() === g.name.trim().toUpperCase());
      return { clientId: saved?.clientId || crypto.randomUUID(), signerId: g.signerId, name: g.name, signaturePngBase64: saved?.signaturePngBase64 || '', selected: true };
    });
  }

  get totalSigners() { return this.input.occupants.length + 1; }
  get signedCount() { return Number(!!this.input.primarySignaturePngBase64) + this.input.occupants.filter(o => !!o.signaturePngBase64).length; }
  get currentSigner() { return this.activeSigner === -1 ? this.input.primaryGuestName : this.input.occupants[this.activeSigner ?? -1]?.name; }
  invalidatePreview() { this.previewReady = false; this.confirmSend = false; this.clearPreview(); }

  addOccupant() {
    if (this.input.occupants.length < 8) {
      this.input.occupants.push({ clientId: crypto.randomUUID(), name: '', signaturePngBase64: '', selected: true });
      this.invalidatePreview();
    }
  }
  canRemove(i: number) { return !this.input.occupants.slice(i).some(o => !!o.signaturePngBase64); }
  removeOccupant(i: number) { if (this.canRemove(i)) { this.input.occupants.splice(i, 1); this.invalidatePreview(); } }
  startSignatures() {
    if (this.input.occupants.some(o => o.selected && !o.name.trim())) {
      this.setMsg('Capture el nombre de los firmantes seleccionados o quite su selección.', true); return;
    }
    this.step = 3; this.activeSigner = null; this.invalidatePreview(); this.msg('');
  }
  selectSigner(i: number) {
    if (this.working || (i === -1 ? !!this.input.primarySignaturePngBase64 || !this.primarySelected : !this.input.occupants[i]?.selected || !!this.input.occupants[i]?.signaturePngBase64)) return;
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
        this.setMsg(`Documento enviado a OPERA. Attachment ID: ${r.attachmentId}.${mail}`, r.emailStatus === 'QueueFailed');
        this.confirmSend = false; this.working = false;
      },
      e => { this.setMsg(typeof e?.error === 'string' ? e.error : e.message, true); this.working = false; }
    );
  }
  lookupGuest() {
    if (!this.operaGiven.trim() || !this.operaSurname.trim()) { this.setMsg('Capture nombre y apellido del acompañante.', true); return; }
    this.operaWorking = true; this.lookup = null; this.guestPreview = null; this.confirmOpera = false;
    this.api.lookupAccompanyingAdult(this.confirmation, this.operaGiven, this.operaSurname).then(
      l => { this.lookup = l; this.operaWorking = false; },
      e => { this.setMsg(e?.error?.message || e.message, true); this.operaWorking = false; }
    );
  }
  previewGuest(pid: string) {
    this.operaProfileId = pid; this.guestPreview = null; this.confirmOpera = false; this.operaWorking = true;
    this.api.previewAddAccompanyingAdult(this.confirmation, pid).then(
      p => { this.guestPreview = p; this.operaWorking = false; },
      e => { this.setMsg(e?.error?.message || e.message, true); this.operaWorking = false; }
    );
  }
  applyGuest() {
    if (!this.guestPreview?.canApply || !this.confirmOpera) return;
    this.operaWorking = true;
    this.api.addAccompanyingAdult(this.confirmation, this.operaProfileId, this.guestPreview.lastModifyDateTime, 'AGREGAR ACOMPAÑANTE').then(
      r => {
        const g = r.after.requestedProfile;
        if (!this.input.occupants.some(o => o.signerId === g.profileId))
          this.input.occupants.push({ name: g.fullName, signerId: g.profileId, signaturePngBase64: '', selected: true });
        if (this.reservation) this.reservation.roomStay.adultCount = r.after.currentAdults;
        this.guestPreview = null; this.confirmOpera = false; this.operaWorking = false;
        this.setMsg(`${g.fullName} fue agregado y verificado en OPERA. Ya puede firmar.`, false);
      },
      e => { this.setMsg(e?.error?.message || e.message, true); this.operaWorking = false; }
    );
  }
  createGuest() {
    const pv = this.lookup?.newProfilePreview;
    if (!pv?.canApply || !this.confirmOpera) return;
    this.operaWorking = true;
    this.api.createAndAddAccompanyingAdult(this.confirmation, this.operaGiven, this.operaSurname, pv.lastModifyDateTime, 'AGREGAR ACOMPAÑANTE').then(
      r => {
        const g = r.after.requestedProfile;
        if (!this.input.occupants.some(o => o.signerId === g.profileId))
          this.input.occupants.push({ name: g.fullName, signerId: g.profileId, signaturePngBase64: '', selected: true });
        if (this.reservation) this.reservation.roomStay.adultCount = r.after.currentAdults;
        this.lookup = null; this.operaGiven = ''; this.operaSurname = ''; this.confirmOpera = false; this.operaWorking = false;
        this.setMsg(`Profile ID ${g.profileId} creado para ${g.fullName} y vinculado en OPERA.`, false);
      },
      e => { this.setMsg(e?.error?.message || e.message, true); this.operaWorking = false; }
    );
  }
  dash(v?: string) { return v?.trim() ? v : '—'; }
  private msg(t: string) { this.message = t; this.isError = false; }
  private setMsg(t: string, err: boolean) { this.message = t; this.isError = err; }
}
