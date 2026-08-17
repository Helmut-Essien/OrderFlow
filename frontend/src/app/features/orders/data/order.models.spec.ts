import { nextOrderActions } from './order.models';

describe('nextOrderActions', () => {
  it('does not allow Pending to jump to Paid', () => {
    expect(nextOrderActions('Pending').map((a) => a.status)).toEqual(['Confirmed', 'Cancelled']);
  });

  it('returns no actions for terminal statuses', () => {
    expect(nextOrderActions('Fulfilled')).toEqual([]);
    expect(nextOrderActions('Cancelled')).toEqual([]);
  });
});
