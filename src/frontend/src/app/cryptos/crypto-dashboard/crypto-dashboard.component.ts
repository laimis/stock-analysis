import {Component, OnInit} from '@angular/core';
import {Router} from '@angular/router';
import {OwnedCrypto, StocksService} from '../../services/stocks.service';


@Component({
    selector: 'app-crypto-dashboard',
    templateUrl: './crypto-dashboard.component.html',
    styleUrls: ['./crypto-dashboard.component.css'],
    standalone: false
})
export class CryptoDashboardComponent implements OnInit {

    owned: OwnedCrypto[]
    performance: any
    past: any
    loaded: boolean = false

    moneySpentOnTokens: number;
    currentEquity: number;
    profits: number;

    activeTab: string = 'owned'
    sortColumn: string
    sortDirection: number = -1

    constructor(private stocks: StocksService, private router: Router) {
    }

    ngOnInit() {

        this.stocks.getCryptos().subscribe(result => {
            this.owned = result.owned;
            this.performance = result.performance;
            this.past = result.past;
            this.loaded = true;
            this.calculateProperties();
            this.sort("profits")
        }, error => {
            console.log(error);
            this.loaded = false;
        })
    }

    onTokenSelected(token: string) {
        this.router.navigateByUrl('/cryptos/' + token).then()
    }

    isActive(tabName: string) {
        return tabName == this.activeTab
    }

    activateTab(tabName: string) {
        this.activeTab = tabName
    }

    calculateProperties() {

        this.moneySpentOnTokens = 0.0
        this.currentEquity = 0.0

        for (const i of this.owned) {

            this.moneySpentOnTokens += i.cost
            this.currentEquity += i.equity
        }

        this.profits = 0.0
        if (this.moneySpentOnTokens != 0) {
            const made = this.currentEquity - this.moneySpentOnTokens;
            this.profits = made / this.moneySpentOnTokens
        }
    }

    sort(column: string) {

        var func = this.getSortFunc(column);

        if (this.sortColumn != column) {
            this.sortDirection = -1
        } else {
            this.sortDirection *= -1
        }
        this.sortColumn = column

        const finalFunc = (a, b) => {
            const result = func(a, b);
            return result * this.sortDirection
        };

        this.owned.sort(finalFunc)
    }

    private getSortFunc(column: string) {
        switch (column) {
            case "ticker":
                return (a: OwnedCrypto, b: OwnedCrypto) => a.token.localeCompare(b.token)
            case "price":
                return (a: OwnedCrypto, b: OwnedCrypto) => a.price - b.price
            case "averageCost":
                return (a: OwnedCrypto, b: OwnedCrypto) => a.averageCost - b.averageCost
            case "quantity":
                return (a: OwnedCrypto, b: OwnedCrypto) => a.quantity - b.quantity
            case "equity":
                return (a: OwnedCrypto, b: OwnedCrypto) => a.equity - b.equity
            case "profits":
                return (a: OwnedCrypto, b: OwnedCrypto) => a.profits - b.profits
            case "profitsPct":
                return (a: OwnedCrypto, b: OwnedCrypto) => a.profitsPct - b.profitsPct
        }

        console.log("unrecognized sort column " + column)
        return null;
    }
}
