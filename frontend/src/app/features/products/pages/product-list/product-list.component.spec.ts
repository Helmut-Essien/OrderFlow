import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ShopStateService } from '../../../../core/shop/shop-state.service';
import { ProductApi } from '../../data/product.api';
import { ProductListResponse } from '../../data/product.models';
import { ProductListComponent } from './product-list.component';

describe('ProductListComponent', () => {
  let fixture: ComponentFixture<ProductListComponent>;
  let listSpy: jasmine.Spy;

  const emptyPage: ProductListResponse = {
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 20,
    categories: [],
    activeCount: 0
  };

  beforeEach(async () => {
    listSpy = jasmine.createSpy('list').and.returnValue(of(emptyPage));

    await TestBed.configureTestingModule({
      imports: [ProductListComponent],
      providers: [
        provideRouter([]),
        ShopStateService,
        { provide: ProductApi, useValue: { list: listSpy } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductListComponent);
    fixture.detectChanges();
  });

  it('shows the catalog-empty copy when there are no SKUs', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('No products yet');
    expect(text).not.toContain('No matching products');
  });

  it('shows no-match copy when a category filter returns nothing', () => {
    fixture.componentInstance.selectCategory('Beverages');
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('No matching products');
    expect(text).not.toContain('No products yet');
  });
});
