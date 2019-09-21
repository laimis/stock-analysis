import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SoldOptionDetailComponent } from './sold-option-detail.component';

describe('SoldOptionDetailComponent', () => {
  let component: SoldOptionDetailComponent;
  let fixture: ComponentFixture<SoldOptionDetailComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SoldOptionDetailComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SoldOptionDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
