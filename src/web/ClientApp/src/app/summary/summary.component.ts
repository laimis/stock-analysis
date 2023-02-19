import { Component, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { StocksService, ReviewList, PositionInstance } from '../services/stocks.service';

@Component({
  selector: 'app-review',
  templateUrl: './summary.component.html',
  styleUrls: ['./summary.component.css']
})
export class SummaryComponent implements OnInit {
  result: ReviewList
  loaded: boolean = false
  timePeriod: string = 'thisweek'

  constructor(
    private stockService:StocksService,
    private title: Title
    ) { }

  ngOnInit() {
    this.title.setTitle("Review - Nightingale Trading")
    this.loadEntries()
  }

  private loadEntries() {
    this.stockService.getTransactionSummary(this.timePeriod).subscribe((r: ReviewList) => {
      this.loaded = true
      this.result = r
    }, _ => { this.loaded = true})
  }

  periodChanged() {
    this.loadEntries()
  }

  stockTransactionTotal() : number {
    return this.result.stockTransactions.reduce((acc, cur) => acc + cur.amount, 0)
  }

  closedPositionProfit() : number {
    return this.result.closedPositions.reduce((acc, cur) => acc + cur.profit, 0)
  }

  closedPositionRR() : number {
    return this.result.closedPositions.reduce((acc, cur) => acc + cur.rr, 0)
  }

  buys(positionInstance:PositionInstance) {
    return positionInstance.transactions.filter(t => t.type == 'buy')
  }

  sells(positionInstance:PositionInstance) {
    return positionInstance.transactions.filter(t => t.type == 'sell')
  }

  assignGrade(position:PositionInstance, note:string) {
    this.stockService.assignGrade(position.ticker, position.positionId, position.grade, note).subscribe(_ => {
      position.gradeNote = note
    })
  }
}

