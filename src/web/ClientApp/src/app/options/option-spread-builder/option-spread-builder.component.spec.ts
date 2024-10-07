import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OptionSpreadBuilderComponent } from './option-spread-builder.component';

describe('OptionSpreadBuilderComponent', () => {
  let component: OptionSpreadBuilderComponent;
  let fixture: ComponentFixture<OptionSpreadBuilderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OptionSpreadBuilderComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(OptionSpreadBuilderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
