import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-events',
  templateUrl: './events.component.html',
  styleUrls: ['./events.component.css']
})
export class EventsComponent implements OnInit {
  events: object[]

  constructor(
    private stockService:StocksService,
    private route:ActivatedRoute
  ) { }

  ngOnInit() {
    var type = this.route.snapshot.queryParamMap.get("type")
    this.loadEvents(type)
  }

  loadEvents(type:string) {
    this.stockService.getEvents(type).subscribe(r => {
      this.events = r
    })
  }

}
