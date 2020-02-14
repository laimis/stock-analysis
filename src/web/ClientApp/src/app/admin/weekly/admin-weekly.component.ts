import { Component, OnInit } from '@angular/core';
import { StocksService } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-admin-weekly',
  templateUrl: './admin-weekly.component.html',
  styleUrls: ['./admin-weekly.component.css']
})
export class AdminWeeklyComponent implements OnInit {

  constructor(private stockService:StocksService) { }

  ngOnInit() {
  }

  kickOff() {
    this.stockService.weeklyReview().subscribe(
      _ => console.log("success"),
      _ => console.log("failure")
    )
  }

}
