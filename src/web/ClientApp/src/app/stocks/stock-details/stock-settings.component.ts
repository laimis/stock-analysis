import { Component, Input } from '@angular/core';
import { OwnedStock, StocksService, GetErrors } from '../../services/stocks.service';

@Component({
  selector: 'stock-settings',
  templateUrl: './stock-settings.component.html',
  styleUrls: ['./stock-settings.component.css']
})

export class StockSettingsComponent {
  category: string
  success: boolean
  ticker: string
  errors: any
  price: number
  cost: number
  percentage: number = 5

  @Input()
  set stock(stock: OwnedStock) {
    console.log("seeit price" + stock)
    console.log(stock)
    this.price = stock.price
  }

  @Input()
  set ownership(ownership: OwnedStock) {
    if (ownership == null)
    {
      return
    }

    this.ticker = ownership.ticker
    this.category = ownership.category
    this.cost = ownership.averageCost
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

  priceLevel() : number {
    if (this.cost > 0) return this.cost
    return this.price
  }

  levels() : Array<object> {

    var levels = []

    var initialPrice = this.priceLevel()

    var percentages = []

    for(var i = - this.percentage; i < this.percentage*10; i=i+this.percentage)
    {
      percentages.push(i)
    }

    percentages.push(50)
    percentages.push(100)
    percentages.push(200)

    percentages.forEach(element => {
      var l = {
        description: element + "%",
        price: (initialPrice * element / 100.0) + initialPrice
      }

      levels.push(l)
    });

    return levels
  }
}
