import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminEmailComponent } from './admin-email.component';

describe('AdminEmailComponent', () => {
  let component: AdminEmailComponent;
  let fixture: ComponentFixture<AdminEmailComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ AdminEmailComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(AdminEmailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
