import { Component, OnInit } from '@angular/core';
import { StocksService, OptionDefinition } from '../services/stocks.service';
import { DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-option-sell',
  templateUrl: './option-sell.component.html',
  styleUrls: ['./option-sell.component.css'],
  providers: [DatePipe]
})
export class OptionSellComponent implements OnInit {

  public option : OptionDefinition
  public ticker : string

  constructor(
    private service: StocksService,
    private route: ActivatedRoute,
    private datePipe: DatePipe) { }

  ngOnInit() {
    var ticker = this.route.snapshot.paramMap.get('ticker');
    if (ticker) {
      this.ticker = ticker;
    }

    this.option = new OptionDefinition();

    this.option.filled = Date()
    this.option.filled = this.datePipe.transform(this.option.filled, 'yyyy-MM-dd');
    this.option.expirationDate = Date()
    this.option.expirationDate = this.datePipe.transform(this.option.expirationDate, 'yyyy-MM-dd');
  }

  clearValues() {
    this.ticker = null
    this.option = new OptionDefinition()
  }

  open() {
    this.service.openOption(this.option).subscribe( () => {
      this.clearValues();
    })
  }
}
