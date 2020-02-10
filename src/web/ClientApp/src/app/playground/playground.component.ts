import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-playground',
  templateUrl: './playground.component.html',
  styleUrls: ['./playground.component.css']
})
export class PlaygroundComponent implements OnInit {

  ticker : string

  constructor() { }

  ngOnInit() {
  }

  onTickerSelected(ticker:string) {
    console.log(ticker)
    this.ticker = ticker
  }

}
