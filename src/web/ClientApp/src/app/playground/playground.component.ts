import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {StocksService} from '../services/stocks.service';
import {GetErrors} from "../services/utils";
import {forkJoin} from "rxjs";
import {tap} from "rxjs/operators";

@Component({
  selector: 'app-playground',
  templateUrl: './playground.component.html',
  styleUrls: ['./playground.component.css']
})
export class PlaygroundComponent implements OnInit {
  tickers: string[];

  constructor(
    private stocks: StocksService,
    private route: ActivatedRoute) {
  }

  errors: string[];
  status: string;
  testTicker: string;

  ngOnInit() {
    const tickerParam = this.route.snapshot.queryParamMap.get('tickers')
    this.tickers = tickerParam ? tickerParam.split(',') : ['AMD']
    this.testTicker = this.tickers[0]
  }

  run() {

    this.status = "Running"

    let positionReport = this.stocks.reportPositions().pipe(
      tap((data) => {
        this.status = "Positions done"
        console.log("Positions")
        console.log(data)
      })
    )

    let singleBarDaily = this.stocks.reportOutcomesSingleBarDaily(this.tickers).pipe(
      tap((data) => {
        this.status = "Single bar daily done"
        console.log("Single bar daily")
        console.log(data)
      }, (error) => {
        console.log("Single bar daily error")
        this.errors = GetErrors(error)
      })
    )
      
      let multiBarDaily = this.stocks.reportOutcomesAllBars(this.tickers).pipe(
          tap((data) => {
          this.status = "Multi bar daily done"
          console.log("Multi bar daily")
          console.log(data)
        }, (error) => {
          console.log("Multi bar daily error")
          this.errors = GetErrors(error)
        })
      )

    let singleBarWeekly = this.stocks.reportOutcomesSingleBarWeekly(this.tickers).pipe(
      tap((data) => {
        this.status = "Single bar weekly done"
        console.log("Single bar weekly")
        console.log(data)
      })
    )

    let gapReport = this.stocks.reportTickerGaps(this.tickers[0]).pipe(
      tap((data) => {
        this.status = "Gaps done"
        console.log("Gaps")
        console.log(data)
      })
    )

    this.status = "Running..."

    forkJoin([positionReport, gapReport, singleBarDaily, singleBarWeekly, multiBarDaily]).subscribe(
      (_) => {
        // this.status = "Done"
        console.log("Main Done")
      },
      (error) => {
        this.errors = GetErrors(error)
      }
    )
  }

  brokerageOrderEntered($event) {
    this.status = "Brokerage order entered: " + $event
  }
}
