import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ShopStateService } from '../../../../core/shop/shop-state.service';
import { OrderApi } from '../../data/order.api';
import { OrderListResponse } from '../../data/order.models';
import { OrderListComponent } from './order-list.component';

describe('OrderListComponent', () => {
  let fixture: ComponentFixture<OrderListComponent>;
  let listSpy: jasmine.Spy;

  const emptyPage: OrderListResponse = {
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 20
  };

  beforeEach(async () => {
    listSpy = jasmine.createSpy('list').and.returnValue(of(emptyPage));

    await TestBed.configureTestingModule({
      imports: [OrderListComponent],
      providers: [
        provideRouter([]),
        ShopStateService,
        { provide: OrderApi, useValue: { list: listSpy } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(OrderListComponent);
    fixture.detectChanges();
  });

  it('shows the empty-shop copy when there are no orders', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('No orders yet');
    expect(text).not.toContain('No matching orders');
  });

  it('shows no-match copy when a status filter returns nothing', () => {
    fixture.componentInstance.selectStatus('Pending');
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('No matching orders');
    expect(text).not.toContain('No orders yet');
  });
});
