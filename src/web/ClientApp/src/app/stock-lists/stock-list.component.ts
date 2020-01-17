import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-stock-list',
  template: `<table class="table">
  <thead>
    <tr>
      <th>Ticker</th>
      <th>Name</th>
      <th>Latest Price</th>
      <th>Time</th>
      <th>Market Cap</th>
      <th>Volume</th>
      <th>PE Ratio</th>
    </tr>
  </thead>
  <tbody>
    <tr *ngFor="let entry of list">
      <td><a [routerLink]="[ '/stocks', entry.symbol ]">{{ entry.symbol }}</a></td>
      <td>{{ entry.companyName }}</td>
      <td>{{ entry.latestPrice | currency }}</td>
      <td>{{ entry.latestTime }}</td>
      <td>{{ entry.marketCap | number }}</td>
      <td>{{ entry.volume | number }}</td>
      <td>{{ entry.peRatio | number }}</td>
    </tr>
  </tbody>
  <tfoot>
    <tr><td colspan="7"></td></tr>
  </tfoot>
</table>
  `
})
export class StockListComponent implements OnInit {
  ngOnInit(): void {
  }

  @Input() list: object[];

}
