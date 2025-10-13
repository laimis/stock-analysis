import { Component, OnInit, inject } from '@angular/core';
import {StocksService} from 'src/app/services/stocks.service';

@Component({
    selector: 'app-recentsells',
    templateUrl: './recentsells.component.html',
    styleUrls: ['./recentsells.component.css'],
    standalone: false
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
