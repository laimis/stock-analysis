import { Component, OnInit } from '@angular/core';
import { StocksService } from '../../services/stocks.service';

@Component({
  selector: 'app-options',
  templateUrl: './option-dashboard.component.html',
  styleUrls: ['./option-dashboard.component.css']
})
export class OptionsComponent implements OnInit {

  statsContainer: any

  errors: string[]
  options: any

  openPositions: any

  activeTab: string = 'performance'

  constructor(
    private service: StocksService
  ) { }

  ngOnInit() {
    this.getOptions()
  }

  getOptions(){
    this.service.getOptions().subscribe( result => {
      this.statsContainer = result
      this.options = result.options
      this.openPositions = result.openPositions
    })
  }

  isActive(tabName:string) {
    return tabName == this.activeTab
  }

  activateTab(tabName:string) {
    this.activeTab = tabName
  }
}
