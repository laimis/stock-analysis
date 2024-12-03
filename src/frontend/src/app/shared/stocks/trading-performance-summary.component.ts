import {Component, Input} from '@angular/core';
import {LabelWithFrequency, StockTradingPerformance} from 'src/app/services/stocks.service';


@Component({
    selector: 'app-trading-performance-summary',
    templateUrl: './trading-performance-summary.component.html',
    standalone: false
})
export class TradingPerformanceSummaryComponent {

    @Input()
    public title: string;

    @Input()
    public performance: StockTradingPerformance

    getPercentageOfGrade(grade: LabelWithFrequency): number {
        let total = this.performance.gradeDistribution.map(g => g.frequency).reduce((a, b) => a + b, 0);
        return grade.frequency / total;
    }
}

