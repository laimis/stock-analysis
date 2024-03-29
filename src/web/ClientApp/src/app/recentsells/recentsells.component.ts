import {Component, OnInit} from '@angular/core';
import {StocksService} from 'src/app/services/stocks.service';

@Component({
    selector: 'app-recentsells',
    templateUrl: './recentsells.component.html',
    styleUrls: ['./recentsells.component.css']
})
export class RecentSellsComponent implements OnInit {
    sells: any;

    constructor(private service: StocksService) {
    }

    ngOnInit(): void {
        this.service.recentSells().subscribe(result => {
            this.sells = result.sells
        }, error => {
            console.log("failed: " + error);
        })
    }

}
