import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NEVER } from 'rxjs';
import { ProductApi } from '../../../products/data/product.api';
import { OrderApi } from '../../data/order.api';
import { OrderFormComponent } from './order-form.component';

describe('OrderFormComponent', () => {
  let fixture: ComponentFixture<OrderFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OrderFormComponent],
      providers: [
        provideRouter([]),
        { provide: OrderApi, useValue: { create: () => NEVER } },
        { provide: ProductApi, useValue: { list: () => NEVER } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(OrderFormComponent);
    fixture.detectChanges();
  });

  it('shows searching, not a false miss, before the first catalog page returns', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Searching…');
    expect(text).not.toContain('No active products match that search.');
    expect(text).not.toContain('No products yet');
  });
});
