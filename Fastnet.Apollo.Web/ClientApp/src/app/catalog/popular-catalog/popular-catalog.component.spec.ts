import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PopularCatalogComponent } from './popular-catalog.component';

describe('PopularCatalogComponent', () => {
  let component: PopularCatalogComponent;
  let fixture: ComponentFixture<PopularCatalogComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PopularCatalogComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PopularCatalogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
