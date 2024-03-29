import {ComponentFixture, TestBed} from '@angular/core/testing';

import {TradesReportComponent} from './trades-report.component';

describe('TradesReportComponent', () => {
    let component: TradesReportComponent;
    let fixture: ComponentFixture<TradesReportComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [TradesReportComponent]
        })
            .compileComponents();

        fixture = TestBed.createComponent(TradesReportComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
