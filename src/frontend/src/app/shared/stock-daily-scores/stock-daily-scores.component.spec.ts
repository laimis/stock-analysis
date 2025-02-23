import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StockDailyScoresComponent } from './stock-daily-scores.component';

describe('StockDailyScoresComponent', () => {
  let component: StockDailyScoresComponent;
  let fixture: ComponentFixture<StockDailyScoresComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StockDailyScoresComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(StockDailyScoresComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
