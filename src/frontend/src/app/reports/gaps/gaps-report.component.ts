import { Component, OnInit, inject } from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {StockGaps, StocksService} from '../../services/stocks.service';
import {GetErrors} from "../../services/utils";

@Component({
    selector: 'app-gaps-report',
    templateUrl: './gaps-report.component.html',
    styleUrls: ['./gaps-report.component.css'],
    standalone: false
})
export class GapsReportComponent implements OnInit {
    private stocksService = inject(StocksService);
    private route = inject(ActivatedRoute);


    errors: string[] = null;
    gaps: StockGaps;

    /** Inserted by Angular inject() migration for backwards compatibility */
    constructor(...args: unknown[]);

    constructor() {
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
