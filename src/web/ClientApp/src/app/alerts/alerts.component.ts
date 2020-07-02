import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { StocksService } from '../services/stocks.service';


@Component({
  selector: 'app-alerts',
  templateUrl: './alerts.component.html',
  styleUrls: ['./alerts.component.css']
})
export class AlertsComponent implements OnInit {

	public alerts : any
  public loaded : boolean = false

  constructor(private stocks : StocksService)
	{ }

	ngOnInit() {

		this.stocks.getAlerts('').subscribe(result => {
      this.alerts = result;
      this.loaded = true;
		}, error => {
			console.log(error);
			this.loaded = false;
    })
  }

  below(price:number, pricePoints:any[]){
    return pricePoints.filter( (v,i,a) => {
      return (v.value < price)
    })
  }

  above(price:number, pricePoints:any[]){
    return pricePoints.filter( (v,i,a) => {
      return (v.value > price)
    })
  }
}
