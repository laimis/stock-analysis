import { Component, OnInit } from '@angular/core';
import { StocksService, OptionDetail, OptionDefinition, OptionGroup } from '../services/stocks.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-option-detail',
  templateUrl: './option-detail.component.html',
  styleUrls: ['./option-detail.component.css']
})
export class OptionDetailComponent implements OnInit {
  ticker: string;

  public options : OptionGroup[]
  public filteredOptions : OptionGroup[]

  public expirations: string[]
  public loading : boolean = true

  public expirationSelection : string = ""
  public sideSelection : string = ""

  constructor(
    private service: StocksService,
    private route: ActivatedRoute) { }

  ngOnInit() {
    var ticker = this.route.snapshot.paramMap.get('ticker');
    if (ticker) {
      this.ticker = ticker;
    }

    this.service.getOptions(this.ticker).subscribe( result => {
      this.options = result.options
      this.expirations = result.expirations
      this.runFilter()
    });
  }

  runFilter() {
    console.log("running filter")
    console.log("expiration: " + this.expirationSelection)
    console.log("side: " + this.sideSelection)
    this.filteredOptions = this.options.filter(this.includeOption, this);
    this.loading = false;
    console.log("filter running finished")
  }

  includeOption(element:OptionDefinition, index, array) {
    if (this.expirationSelection !== "") {
      if (element.expiration != this.expirationSelection) {
        console.log("filterig out expiration " + element.expiration)
        return false
      }
    }

    if (this.sideSelection !== "") {
      if (element.optionType != this.sideSelection) {
        console.log("filterig out side " + element.optionType)
        return false
      }
    }

    return true
  }

  onExpirationChange(newValue) {
    console.log(newValue)
    this.expirationSelection = newValue
    this.runFilter()
  }

  onSideChange(newValue) {
    console.log(newValue)
    this.sideSelection = newValue
    this.runFilter()
  }

}
