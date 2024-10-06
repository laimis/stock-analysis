import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {StockGaps, StocksService} from '../../services/stocks.service';
import {GetErrors} from "../../services/utils";

@Component({
    selector: 'app-gaps-report',
    templateUrl: './gaps-report.component.html',
    styleUrls: ['./gaps-report.component.css']
})
export class GapsReportComponent implements OnInit {

    errors: string[] = null;
    gaps: StockGaps;

    constructor(
        private stocksService: StocksService,
        private route: ActivatedRoute) {
    }

    ngOnInit(): void {
        this.route.queryParams.subscribe(queryParams => {
            const tickerParam = queryParams['ticker'];
            if (tickerParam) {
                this.stocksService.reportTickerGaps(tickerParam).subscribe(data => {
                    this.gaps = data
                }, error => {
                    this.errors = GetErrors(error);
                });
            } else {
                this.errors = ["ticker query param missing"];
            }
        })
    }
}
