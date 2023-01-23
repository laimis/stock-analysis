import { Component, Input } from '@angular/core';
import { PositionInstance, TradingStrategyResults } from 'src/app/services/stocks.service';


@Component({
  selector: 'app-trading-actual-vs-simulated',
  templateUrl: './trading-actual-vs-simulated.component.html',
})
export class TradingActualVsSimulatedPositionComponent {

  showDetails: boolean = false;

  @Input()
  public simulations: TradingStrategyResults

  toggleShowDetails() {
    this.showDetails = !this.showDetails;
  }

  sortedResults() {
    return this.simulations.results.sort((a, b) => b.position.combinedProfit - a.position.combinedProfit);
  }
}

