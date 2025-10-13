import { Component, OnInit, inject } from '@angular/core';
import {StocksService} from '../services/stocks.service';
import {ActivatedRoute} from '@angular/router';
import {GetErrors} from "../services/utils";
import { ErrorDisplayComponent } from "../shared/error-display/error-display.component";

@Component({
    selector: 'app-events',
    templateUrl: './events.component.html',
    styleUrls: ['./events.component.css'],
    imports: [ErrorDisplayComponent]
})
export class EventsComponent implements OnInit {
    private stockService = inject(StocksService);
    private route = inject(ActivatedRoute);

    events: any[]
    errors = null


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
