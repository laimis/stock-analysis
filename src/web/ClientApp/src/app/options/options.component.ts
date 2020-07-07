import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';

@Component({
  selector: 'app-options',
  templateUrl: './options.component.html',
  styleUrls: ['./options.component.css']
})
export class OptionsComponent implements OnInit {

  errors: string[]
  options: any
  overall: any
  buy: any
  sell: any

  constructor(
    private service: StocksService
  ) { }

  ngOnInit() {
    this.getOptions()
  }

  getOptions(){
    this.service.getOptions().subscribe( result => {
      this.options = result.options
      this.overall = result.overall
      this.buy = result.buy
      this.sell = result.sell
    })
  }
}
