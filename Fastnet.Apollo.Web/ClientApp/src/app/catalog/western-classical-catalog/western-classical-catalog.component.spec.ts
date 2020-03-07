import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { WesternClassicalCatalogComponent } from './western-classical-catalog.component';

describe('WesternClassicalCatalogComponent', () => {
  let component: WesternClassicalCatalogComponent;
  let fixture: ComponentFixture<WesternClassicalCatalogComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ WesternClassicalCatalogComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(WesternClassicalCatalogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
