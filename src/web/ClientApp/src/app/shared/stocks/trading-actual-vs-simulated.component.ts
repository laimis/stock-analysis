import { Component, Input } from '@angular/core';
import { TradingStrategyResults } from 'src/app/services/stocks.service';


@Component({
  selector: 'app-trading-actual-vs-simulated',
  templateUrl: './trading-actual-vs-simulated.component.html',
  styleUrls: ['./trading-actual-vs-simulated.component.css']
})
export class TradingActualVsSimulatedPositionComponent {

  showDetails: number | null = null;

  @Input()
  simulations: TradingStrategyResults

  @Input()
  simulationErrors: string[];

  toggleShowDetails(index: number) {
    if (this.showDetails == index) {
      this.showDetails = null;
    } else {
      this.showDetails = index;
    }
  }

  sortedResults() {
    return this.simulations.results.sort((a, b) => b.position.profit - a.position.profit);
  }
}

