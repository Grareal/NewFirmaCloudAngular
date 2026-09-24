import { reconcileOccupants } from './tarjeta';
import { Reservation } from '../../core/models';

const reservation = (overrides: Partial<Reservation> = {}) => ({
  confirmationNumber: '123',
  hotelId: 'HOTEL',
  reservationStatus: 'Reserved',
  reservationIdList: [],
  guest: {
    id: 'PRIMARY', fullName: 'María López', givenName: 'María', surname: 'López',
    email: '', phoneNumber: '', language: 'es', address: { city: '', stateProvCode: '', countryCode: '' }
  },
  roomStay: {
    roomId: '', roomType: '', roomClass: '', arrivalDate: '', departureDate: '', adultCount: 2,
    childCount: 0, rateAmount: 0, currencyCode: '', ratePlanCode: '', guaranteeCode: '', guaranteeDescription: ''
  },
  userDefinedFields: [],
  accompanyingGuests: [],
  accompanyingGuestNames: [],
  companions: [],
  ...overrides
}) satisfies Reservation;

describe('reconcileOccupants', () => {
  it('does not duplicate a guest exposed in both OPERA collections', () => {
    const result = reconcileOccupants(reservation({
      accompanyingGuests: [{ fullName: 'Juan Pérez', profileId: 'P-1' }],
      accompanyingGuestNames: ['  JUAN   PEREZ  ']
    }), [], () => 'new-id');

    expect(result).toHaveLength(1);
    expect(result[0]).toMatchObject({ name: 'Juan Pérez', signerId: 'P-1', clientId: 'new-id' });
  });

  it('excludes the primary guest and keeps the saved signature for the companion', () => {
    const result = reconcileOccupants(reservation({
      accompanyingGuests: [
        { fullName: 'Maria Lopez', profileId: 'PRIMARY' },
        { fullName: 'Ana Ruiz', profileId: 'P-2' }
      ],
      accompanyingGuestNames: ['María López', 'Ana Ruiz']
    }), [{ clientId: 'saved-id', signerId: 'P-2', name: 'ANA RUIZ', signaturePngBase64: 'png', selected: true }]);

    expect(result).toHaveLength(1);
    expect(result[0]).toMatchObject({ name: 'Ana Ruiz', clientId: 'saved-id', signaturePngBase64: 'png' });
  });

  it('does not create empty rows from the adult count', () => {
    expect(reconcileOccupants(reservation(), [])).toEqual([]);
  });
});
