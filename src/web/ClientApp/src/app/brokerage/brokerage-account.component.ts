import {Component, Input} from '@angular/core';
import {BrokerageAccount} from "../services/stocks.service";

@Component({
  selector: 'app-brokerage-account',
  templateUrl: './brokerage-account.component.html',
  styleUrl: './brokerage-account.component.css'
})
export class BrokerageAccountComponent {

  @Input() account: BrokerageAccount;

  private sortColumn: string = 'ticker'
  private sortDirection: string = 'asc'

  sort(column:string) {
    if (this.sortColumn == column) {
      this.sortDirection = this.sortDirection == 'asc' ? 'desc' : 'asc'
    } else {
      this.sortColumn = column
      this.sortDirection = 'asc'
    }

    this.account.stockPositions.sort((a, b) => {
      if (this.sortColumn === 'total') {
        const aTotal = a.quantity * a.averageCost;
        const bTotal = b.quantity * b.averageCost;

        if (this.sortDirection == 'asc') {
          return aTotal > bTotal ? 1 : -1
        } else {
          return aTotal < bTotal ? 1 : -1
        }
      } else {
        if (this.sortDirection == 'asc') {
          return a[this.sortColumn] > b[this.sortColumn] ? 1 : -1
        } else {
          return a[this.sortColumn] < b[this.sortColumn] ? 1 : -1
        }
      }
    })
  }
}
