import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';

@Component({
  selector: 'app-stock-mostactive',
  templateUrl: './stock-mostactive.component.html',
  styleUrls: ['./stock-mostactive.component.css']
})
export class StockMostactiveComponent implements OnInit {

  public loaded = false
  public mostActive:object[]

  constructor(
    private service: StocksService
  ) { }

  ngOnInit() {
    this.service.getMostActive().subscribe( result => {
      this.mostActive = result
      this.loaded = true
    })
  }

}
