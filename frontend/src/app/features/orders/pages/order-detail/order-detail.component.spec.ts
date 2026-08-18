import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { OrderApi } from '../../data/order.api';
import { OrderDto } from '../../data/order.models';
import { OrderDetailComponent } from './order-detail.component';

describe('OrderDetailComponent', () => {
  let fixture: ComponentFixture<OrderDetailComponent>;
  let changeStatusSpy: jasmine.Spy;

  const pendingOrder: OrderDto = {
    id: '01orderpending000000000001',
    shopId: '01shop00000000000000000001',
    customerName: 'Ama Mensah',
    status: 'Pending',
    source: 'Manual',
    needsClarification: false,
    totalAmount: 10,
    version: 1,
    createdAt: '2026-08-17T10:00:00Z',
    updatedAt: '2026-08-17T10:00:00Z',
    lines: []
  };

  beforeEach(async () => {
    changeStatusSpy = jasmine.createSpy('changeStatus');

    await TestBed.configureTestingModule({
      imports: [OrderDetailComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: { get: () => pendingOrder.id } },
            // OrderDetailComponent subscribes to `route.paramMap`; tests must provide it.
            paramMap: of({ get: () => pendingOrder.id } as any)
          }
        },
        {
          provide: OrderApi,
          useValue: {
            get: () => of(pendingOrder),
            changeStatus: changeStatusSpy
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(OrderDetailComponent);
    fixture.detectChanges();
  });

  it('does not POST cancel until the shop confirms', () => {
    const cancel = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>
    ).find((button) => button.textContent?.trim() === 'Cancel');
    expect(cancel).toBeTruthy();
    cancel?.click();
    fixture.detectChanges();

    expect(changeStatusSpy).not.toHaveBeenCalled();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Cancel this order?');
    expect(text).toContain('This order has not reserved stock');
  });
});
