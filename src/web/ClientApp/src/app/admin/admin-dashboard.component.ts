import {Component} from '@angular/core';
import {StocksService} from 'src/app/services/stocks.service';

@Component({
    selector: 'app-admin-dashboard',
    templateUrl: './admin-dashboard.component.html',
    styleUrls: ['./admin-dashboard.component.css']
})
export class AdminDashboardComponent {

    constructor(private stockService: StocksService) {
    }

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
