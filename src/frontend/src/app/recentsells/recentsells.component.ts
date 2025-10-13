import { DatePipe, NgClass, PercentPipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import {StocksService} from 'src/app/services/stocks.service';
import { StockLinkComponent } from "../shared/stocks/stock-link.component";

@Component({
    selector: 'app-recentsells',
    templateUrl: './recentsells.component.html',
    styleUrls: ['./recentsells.component.css'],
    imports: [NgClass, StockLinkComponent, DatePipe, PercentPipe]
})
export class RecentSellsComponent implements OnInit {
    private service = inject(StocksService);

    sells: any;


    ngOnInit(): void {
        this.service.recentSells().subscribe(result => {
            this.sells = result.sells
        }, error => {
            console.log("failed: " + error);
        })
    }

}
