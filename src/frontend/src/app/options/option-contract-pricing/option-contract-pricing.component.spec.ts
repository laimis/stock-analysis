import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OptionContractPricingComponent } from './option-contract-pricing.component';

describe('OptionContractsChartsComponent', () => {
  let component: OptionContractPricingComponent;
  let fixture: ComponentFixture<OptionContractPricingComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OptionContractPricingComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(OptionContractPricingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
