import { Component, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { StocksService, TransactionList, ReviewList } from '../services/stocks.service';

@Component({
  selector: 'app-review',
  templateUrl: './review.component.html',
  styleUrls: ['./review.component.css']
})
export class ReviewComponent implements OnInit {
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
    this.stockService.getReviewEntires(this.timePeriod).subscribe((r: ReviewList) => {
      this.loaded = true
      this.result = r
    }, _ => { this.loaded = true})
  }

  periodChanged() {
    this.loadEntries()
  }
}

