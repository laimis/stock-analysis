import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { StockGaps, StocksService } from '../../services/stocks.service';

@Component({
  selector: 'app-gaps-report',
  templateUrl: './gaps-report.component.html',
  styleUrls: ['./gaps-report.component.css']
})
export class GapsReportComponent implements OnInit {
  
  error: string = null;
  gaps: StockGaps;
  
  constructor (
    private stocksService: StocksService,
    private route: ActivatedRoute) {
  }
  
  ngOnInit(): void {
    var tickerParam = this.route.snapshot.queryParamMap.get("ticker");
    if (tickerParam) {
      this.stocksService.reportTickerGaps(tickerParam).subscribe(data => {
        this.gaps = data
      });
    } else {
      this.error = "ticker query param missing";
    }
  }
}
