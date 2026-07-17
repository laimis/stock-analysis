import { CurrencyPipe, DecimalPipe, PercentPipe } from '@angular/common';
import {Component, Input, OnChanges, OnInit, ChangeDetectionStrategy} from '@angular/core';
import { OptionPerformanceView, OptionTradePerformanceMetrics } from 'src/app/services/option.service';

@Component({
    selector: 'app-option-performance',
    templateUrl: './option-performance.component.html',
    styleUrls: ['./option-performance.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [
        PercentPipe,
        CurrencyPipe,
        DecimalPipe        
    ]
})

export class OptionPerformanceComponent implements OnInit, OnChanges {

    @Input() performance: OptionPerformanceView;
  
  selectedTimeframe: string = 'YTD';
  selectedView: string = 'overall';
  
  currentMetrics: OptionTradePerformanceMetrics;
  timeframeOptions: string[] = [];
  
  ngOnInit() {
    // Default initialization
    this.updateCurrentMetrics();
  }
  
  ngOnChanges() {
    if (this.performance) {
      this.timeframeOptions = Object.keys(this.performance.byTimeframes);
      this.updateCurrentMetrics();
    }
  }
  
  updateCurrentMetrics() {
    if (!this.performance) return;
    
    switch (this.selectedView) {
      case 'timeframe':
        this.currentMetrics = this.performance.byTimeframes[this.selectedTimeframe] || this.performance.total;
        break;
      default:
        this.currentMetrics = this.performance.total;
    }
  }
  
  onViewChange(view: string) {
    this.selectedView = view;
    this.updateCurrentMetrics();
  }
  
  onTimeframeChange(timeframe: string) {
    this.selectedTimeframe = timeframe;
    if (this.selectedView === 'timeframe') {
      this.updateCurrentMetrics();
    }
  }
}
