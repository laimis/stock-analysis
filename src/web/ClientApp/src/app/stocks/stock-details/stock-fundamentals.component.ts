import { Component, Input } from '@angular/core';
import { StockDetails, StockProfile } from '../../services/stocks.service';

@Component({
  selector: 'app-stock-fundamentals',
  templateUrl: './stock-fundamentals.component.html',
  styleUrls: ['./stock-fundamentals.component.css']
})

export class StockFundamentalsComponent {

  public summary : StockDetails;
  public profile: StockProfile;

  @Input()
  set stock(stock: StockDetails) {
    this.profile = stock.profile;
    this.summary = stock
  }
  get stock(): StockDetails { return this.summary; }

	constructor(){}

	ngOnInit(): void {}
}
