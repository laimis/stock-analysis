import { Component, inject } from '@angular/core';
import {StocksService} from 'src/app/services/stocks.service';

@Component({
    selector: 'app-admin-dashboard',
    templateUrl: './admin-dashboard.component.html',
    styleUrls: ['./admin-dashboard.component.css'],
    standalone: false
})
export class AdminDashboardComponent {
    private stockService = inject(StocksService);

    turnOffSms() {
        this.stockService.smsOff().subscribe(
            _ => console.log("success"),
            _ => console.log("failure")
        )
    }

    turnOnSms() {
        this.stockService.smsOn().subscribe(
            _ => console.log("success"),
            _ => console.log("failure")
        )
    }

}
