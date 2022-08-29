import { Component, Input } from '@angular/core';
import { StockAdvancedStats, StockDetails, StockProfile } from '../../services/stocks.service';

@Component({
  selector: 'stock-fundamentals',
  templateUrl: './stock-fundamentals.component.html',
  styleUrls: ['./stock-fundamentals.component.css']
})

export class StockFundamentalsComponent {

  public summary : StockDetails;
  public profile: StockProfile;
  public stats: StockAdvancedStats;

  @Input()
  set stock(stock: StockDetails) {
    this.profile = stock.profile;
    this.stats = stock.stats;
    this.summary = stock
  }
  get stock(): StockDetails { return this.summary; }

	constructor(){}

	ngOnInit(): void {}
}
