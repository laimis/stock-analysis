import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';

@Component({
  selector: 'app-stock-lists',
  templateUrl: './stock-lists.component.html',
  styleUrls: ['./stock-lists.component.css']
})
export class StockListsComponent implements OnInit {

  public active:object[]
  public gainers:object[]
  public losers:object[]

  constructor(
    private service: StocksService
  ) { }

  ngOnInit() {
    this.service.getStockLists().subscribe( result => {

      console.log("assigning lists")

      this.active = result.active
      this.gainers = result.gainers
      this.losers = result.losers
    })
  }

}
