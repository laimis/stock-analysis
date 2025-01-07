import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OptionPositionComponent } from './option-position.component';

describe('OptionPositionComponent', () => {
  let component: OptionPositionComponent;
  let fixture: ComponentFixture<OptionPositionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OptionPositionComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(OptionPositionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
