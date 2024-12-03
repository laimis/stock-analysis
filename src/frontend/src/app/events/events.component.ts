import {Component, OnInit} from '@angular/core';
import {StocksService} from '../services/stocks.service';
import {ActivatedRoute} from '@angular/router';
import {GetErrors} from "../services/utils";

@Component({
    selector: 'app-events',
    templateUrl: './events.component.html',
    styleUrls: ['./events.component.css'],
    standalone: false
})
export class EventsComponent implements OnInit {
    events: any[]
    errors = null

    constructor(
        private stockService: StocksService,
        private route: ActivatedRoute
    ) {
    }

    ngOnInit() {
        const type = this.route.snapshot.queryParamMap.get("type");
        this.loadEvents(type)
    }

    loadEvents(type: string) {
        this.stockService.getEvents(type).subscribe(r => {
            this.events = r
        }, err => { 
            this.errors = GetErrors(err)
        })
    }

}
