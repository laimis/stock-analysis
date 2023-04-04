import { Component } from '@angular/core';

@Component({
  selector: 'app-routine',
  templateUrl: './routine.component.html',
  styleUrls: ['./routine.component.css']
})

export class RoutineComponent {
  routineSteps = [
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
      url: ''
    },
    {
      label: 'Review positions/pending orders',
      url: 'https://ngtrading-xiu9e.ondigitalocean.app/stocks/newposition'
    }

  ]
}
