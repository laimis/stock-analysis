import { Component, Input } from '@angular/core';
import { StockSummary } from '../../services/stocks.service';

@Component({
  selector: 'stock-fundamentals',
  templateUrl: './stock-fundamentals.component.html',
  styleUrls: ['./stock-fundamentals.component.css']
})

export class StockFundamentalsComponent {

  public summary : StockSummary;
  public profile: object;
  public stats: object;

  @Input()
  set stock(stock: StockSummary) {
    this.profile = stock.profile;
    this.stats = stock.stats;
    this.summary = stock
  }
  get stock(): StockSummary { return this.summary; }

	constructor(){}

	ngOnInit(): void {}
}
