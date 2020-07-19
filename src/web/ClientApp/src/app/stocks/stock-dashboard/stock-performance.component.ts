import { Component, OnInit, Input } from '@angular/core';
import { StockGridEntry, StocksService } from 'src/app/services/stocks.service';


@Component({
  selector: 'stock-performance',
  templateUrl: './stock-performance.component.html',
  styleUrls: ['./stock-performance.component.css']
})
export class StockOwnershipPerformanceComponent implements OnInit {

	loaded: boolean = false

  ngOnInit() {
  }

  @Input()
  performance: any
}
