import {Component, Input} from '@angular/core';
import {OwnedCrypto} from '../../services/stocks.service';

@Component({
    selector: 'app-crypto-ownership-grid',
    templateUrl: './crypto-ownership-grid.component.html',
    styleUrls: ['./crypto-ownership-grid.component.css']
})
export class CryptoOwnershipGridComponent {

    public loaded: boolean = false;

    @Input() owned: OwnedCrypto[];

    sortColumn: string
    sortDirection: number = -1

    sort(column: string) {

        var func = this.getSortFunc(column);

        if (this.sortColumn != column) {
            this.sortDirection = -1
        } else {
            this.sortDirection *= -1
        }
        this.sortColumn = column

        var finalFunc = (a, b) => {
            var result = func(a, b)
            return result * this.sortDirection
        }

        this.owned.sort(finalFunc)
    }

    ownershipPct(ticker: OwnedCrypto) {
        let total = this.owned
            .map(s => s.cost)
            .reduce((acc, curr) => acc + curr)

        return ticker.cost / total
    }

    equityPct(ticker: OwnedCrypto) {
        let total = this.owned
            .map(s => s.equity)
            .reduce((acc, curr) => acc + curr)

        return ticker.equity / total
    }

    private getSortFunc(column: string) {
        switch (column) {
            case "averageCost":
                return (a: OwnedCrypto, b: OwnedCrypto) => a.averageCost - b.averageCost
            case "price":
                return (a: OwnedCrypto, b: OwnedCrypto) => a.price - b.price
            case "daysheld":
                return (a: OwnedCrypto, b: OwnedCrypto) => a.daysHeld - b.daysHeld
            case "invested":
                return (a: OwnedCrypto, b: OwnedCrypto) => a.cost - b.cost
            case "profits":
                return (a: OwnedCrypto, b: OwnedCrypto) => a.profits - b.profits
            case "equity":
                return (a: OwnedCrypto, b: OwnedCrypto) => a.equity - b.equity
            case "profitsPct":
                return (a: OwnedCrypto, b: OwnedCrypto) => a.profitsPct - b.profitsPct
            case "owned":
                return (a: OwnedCrypto, b: OwnedCrypto) => a.quantity - b.quantity
            case "token":
                return (a: OwnedCrypto, b: OwnedCrypto) => a.token.localeCompare(b.token)
        }

        console.log("unrecognized sort column " + column)
        return null;
    }
}
