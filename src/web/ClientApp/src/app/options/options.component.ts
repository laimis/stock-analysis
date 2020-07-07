import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';

@Component({
  selector: 'app-options',
  templateUrl: './options.component.html'
})
export class OptionsComponent implements OnInit {

  errors: string[]
  options: any

  constructor(
    private service: StocksService
  ) { }

  ngOnInit() {
    this.getOptions()
  }

  getOptions(){
    this.service.getOptions().subscribe( result => {
      this.options = result
    })
  }
}
