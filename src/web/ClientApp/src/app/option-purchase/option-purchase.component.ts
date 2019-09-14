import { Component, OnInit } from '@angular/core';
import { StocksService, OptionDefinition } from '../services/stocks.service';
import { Location, DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-option-purchase',
  templateUrl: './option-purchase.component.html',
  styleUrls: ['./option-purchase.component.css'],
  providers: [DatePipe]
})
export class OptionPurchaseComponent implements OnInit {

  public option : OptionDefinition
  public ticker : string

  constructor(
    private service: StocksService,
    private location: Location,
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

  back() {
    this.location.back();
  }
}
