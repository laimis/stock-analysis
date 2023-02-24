import { Component, Input } from '@angular/core';
import { TradingStrategyResults } from 'src/app/services/stocks.service';


@Component({
  selector: 'app-trading-actual-vs-simulated',
  templateUrl: './trading-actual-vs-simulated.component.html',
  styleUrls: ['./trading-actual-vs-simulated.component.css']
})
export class TradingActualVsSimulatedPositionComponent {

  showDetails: boolean = false;

  @Input()
  simulations: TradingStrategyResults

  @Input()
  simulationErrors: string[];

  toggleShowDetails() {
    this.showDetails = !this.showDetails;
  }

  sortedResults() {
    return this.simulations.results.sort((a, b) => b.position.combinedProfit - a.position.combinedProfit);
  }
}

