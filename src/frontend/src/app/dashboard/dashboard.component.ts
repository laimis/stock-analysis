import {Component, OnInit} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {Router} from '@angular/router';
import {PortfolioHoldings, StocksService} from '../services/stocks.service';


@Component({
    selector: 'app-dashboard',
    templateUrl: './dashboard.component.html',
    styleUrls: ['./dashboard.component.css'],
    standalone: false
})
export class DashboardComponent implements OnInit {

    dashboard: PortfolioHoldings
    loaded: boolean = false

    toolLinks = [
        {path: '/summary', label: 'Weekly Summary'},
        {path: '/trading/review', label: 'Trading Review'},
        {path: '/stocks/analysis', label: 'Stock Position Analysis'},
        {path: '/stocks/tradingsimulations', label: 'Simulations'},
        {path: '/stocks/simulator', label: 'SimulatOR'},
        {path: '/stocks/lists', label: 'Stock Lists'},
        {path: '/reports/recentsells', label: 'Recent Sells'},
        {path: '/transactions', label: 'Transactions'},
        {path: '/reports/chain', label: 'Chain'},
        {path: '/reports/trends', label: 'Trends'},
    ];

    constructor(
        private stocks: StocksService,
        private router: Router,
        private title: Title) {
    }

    ngOnInit() {

        this.title.setTitle("Dashboard - Nightingale Trading")

        this.stocks.getPortfolioHoldings().subscribe(result => {
            this.dashboard = result;
            this.loaded = true;
        }, error => {
            console.log(error);
            this.loaded = false;
        })
    }

    onTickerSelected(ticker: string) {
        this.router.navigateByUrl('/stocks/' + ticker)
    }
}
