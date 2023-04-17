import { Component, OnInit } from '@angular/core';
import { Routine, StocksService } from '../services/stocks.service';
import { GetErrors, toggleVisuallyHidden } from '../services/utils';

@Component({
  selector: 'app-routine',
  templateUrl: './routines-dashboard.component.html',
  styleUrls: ['./routines-dashboard.component.css']
})

export class RoutineDashboardComponent implements OnInit {
  dailyRoutines = [
    {
      label: 'Review stock market conditions by going to TradingView and looking at Indexes list',
      url: 'https://www.tradingview.com/chart/kQn4rgoA/'
    },
    {
      label: 'Review stock positions',
      url: 'https://ngtrading-xiu9e.ondigitalocean.app/trading/analysis'
    },
    {
      label: 'Review Industry Trends',
      url: 'https://localhost:5001/trends'
    },
    {
      label: 'Review Screeners',
      url: 'https://localhost:5001/'
    },
    {
      label: 'Review Alerts: look gmail',
      url: 'https://mail.google.com/mail/u/0/#label/tradeview'
    },
    {
      label: 'Review positions/pending orders',
      url: 'https://ngtrading-xiu9e.ondigitalocean.app/stocks/newposition'
    }
  ]

  weeklyRoutines = [
    {
      label: 'Go to indexes and review SPY, QQQ, IWO, IWM, China, XME, XLE, ARKK, Bitcoin, Gold',
      url: 'https://www.tradingview.com/chart/kQn4rgoA/'
    },
    {
      label: 'Fear and Greed Index (markets)',
      url: 'https://www.cnn.com/markets/fear-and-greed'
    },
    {
      label: 'Fear and Greed Index (crypto)',
      url: 'https://alternative.me/crypto/fear-and-greed-index/'
    },
    {
      label: 'Review grades',
      url: 'https://ngtrading-xiu9e.ondigitalocean.app/trading/review'
    },
    {
      label: 'Earnings list for the week',
      url: 'https://invest.ameritrade.com/grid/p/site#r=home'
    },
    {
      label: 'Review stock positions on TradingView',
      url: 'https://www.tradingview.com/chart/kQn4rgoA/'
    },
    {
      label: 'Review stock positions in portfolio',
      url: 'https://ngtrading-xiu9e.ondigitalocean.app/trading/openreview'
    },
    {
      label: 'Review earnings last week',
      url: 'https://localhost:5001/earnings'
    },
    {
      label: 'Review trends',
      url: 'https://localhost:5001/trends'
    },
    {
      label: 'Review watchlists',
      url: 'https://www.tradingview.com/chart/kQn4rgoA/'
    },
    {
      label: 'Review alerts',
      url: 'https://www.tradingview.com/chart/kQn4rgoA/'
    },
    {
      label: 'Review common mistakes',
      url: ''
    },
    {
      label: 'Remember: 1. Anything can happen. 2. You don’t need to know what will happen to make money. 3. Wins and losses are randomly distributed. 4. Edge means having a higher probability of one thing happening over the other. 5. Every moment in the market is unique'
    }
  ]

  routines:Routine[] = []

  activeRoutine = null
  currentStep = 0

  errors:string[] = null

  constructor(private service:StocksService) { }

  ngOnInit() {
    this.fetchRoutines();
  }

  private fetchRoutines() {
    this.service.getRoutines().subscribe(
      data => {
        this.routines = data;
      }
    );
  }

  activate(routine) {
    this.activeRoutine = routine
    this.currentStep = 0
  }

  deactivate() {
    this.activeRoutine = null
  }

  nextStep() {
    this.currentStep++
  }

  prevStep() {
    this.currentStep--
  }

  reset() {
    this.currentStep = 0
  }

  toggleVisibility(element) {
    toggleVisuallyHidden(element)
  }

  create(name:string, description:string) {
    this.service.createRoutine(name, description).subscribe(
      _ => {
        this.fetchRoutines();
      },
      error => {
        this.errors = GetErrors(error)
      }
    )
  }

  addStep(routine:Routine, label:string, url:string) {
    this.service.addRoutineStep(routine.name, label, url).subscribe(
      _ => {
        this.fetchRoutines();
      },
      error => {
        this.errors = GetErrors(error)
      }
    )
  }

  deleteStep(routine:Routine, stepIndex:number) {
    this.service.deleteRoutineStep(routine.name, stepIndex).subscribe(
      _ => {
        this.fetchRoutines();
      },
      error => {
        this.errors = GetErrors(error)
      }
    )
  }
}
