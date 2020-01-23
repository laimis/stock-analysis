import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';

@Component({
  selector: 'app-events',
  templateUrl: './events.component.html',
  styleUrls: ['./events.component.css']
})
export class EventsComponent implements OnInit {
  events: object[]

  constructor(
    private stockService:StocksService
  ) { }

  ngOnInit() {
    this.loadEvents()
  }

  loadEvents() {
    this.stockService.getEvents().subscribe(r => {
      this.events = r
    })
  }

}
