import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-stock-list',
  template: `<table class="table table-hover border">
  <thead class="thead-light">
    <tr>
      <th>Ticker</th>
      <th>Name</th>
      <th>Latest Price</th>
      <th>Time</th>
      <th style="text-align: right">Market Cap</th>
      <th style="text-align: right">Volume</th>
      <th style="text-align: right">PE Ratio</th>
    </tr>
  </thead>
  <tbody>
    <tr *ngFor="let entry of list">
      <td><a [routerLink]="[ '/stocks', entry.symbol ]">{{ entry.symbol }}</a></td>
      <td>{{ entry.companyName }}</td>
      <td>{{ entry.latestPrice | currency }}</td>
      <td>{{ entry.latestTime }}</td>
      <td align="right">{{ entry.marketCap / 1000000 | number }} M</td>
      <td align="right">{{ entry.volume / 1000000 | number }} M</td>
      <td align="right">{{ entry.peRatio | number }}</td>
    </tr>
  </tbody>
</table>
  `
})
export class StockListComponent implements OnInit {
  ngOnInit(): void {
  }

  @Input() list: object[];

}
