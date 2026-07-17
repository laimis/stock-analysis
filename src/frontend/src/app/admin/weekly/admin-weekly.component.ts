import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import {StocksService} from 'src/app/services/stocks.service';

@Component({
    selector: 'app-admin-weekly',
    templateUrl: './admin-weekly.component.html',
    styleUrls: ['./admin-weekly.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: true
})
export class AdminWeeklyComponent {
    private stockService = inject(StocksService);


    kickOff(everyone: boolean) {
        this.stockService.weeklyReview({everyone: everyone}).subscribe(
            _ => console.log("success"),
            _ => console.log("failure")
        )
    }

}
