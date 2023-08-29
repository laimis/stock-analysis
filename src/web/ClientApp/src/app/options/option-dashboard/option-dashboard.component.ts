import { Component, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { BrokerageOptionPosition, OptionsContainer, StocksService } from '../../services/stocks.service';

@Component({
  selector: 'app-options',
  templateUrl: './option-dashboard.component.html',
  styleUrls: ['./option-dashboard.component.css']
})
export class OptionsComponent implements OnInit {

  optionsContainer: OptionsContainer
  
  loaded: boolean = false;

  activeTab: string = 'open'

  constructor(
    private service: StocksService,
    private title: Title
  ) { }

  ngOnInit() {
    this.title.setTitle("Options - Nightingale Trading")
    this.getOptions()
  }

  getOptions(){
    this.service.getOptions().subscribe( result => {
      this.optionsContainer = result
      this.loaded = true
    })
  }

  isActive(tabName:string) {
    return tabName == this.activeTab
  }

  activateTab(tabName:string) {
    this.activeTab = tabName
  }

  refreshOptions() {
    this.getOptions()
  }
}
