import { Component, Input } from '@angular/core';
import { OwnedStock, StocksService, GetErrors } from '../../services/stocks.service';

@Component({
  selector: 'stock-settings',
  templateUrl: './stock-settings.component.html',
  // styleUrls: ['./stock-notes.component.css']
})

export class StockSettingsComponent {
  category: string;
  success: boolean;
  ticker: string;
  errors: any;

  @Input()
  set stock(stock: OwnedStock) {
    this.ticker = stock.ticker
    this.category = stock.category
  }

	constructor(private service: StocksService) { }

  ngOnInit(): void {}

  saveSettings(): void {
    this.success = false

    this.service.settings(this.ticker,this.category).subscribe( r => {
      this.success = true
    }, err => {
      this.errors = GetErrors(err)
    })
  }
}
