import { Component, OnInit } from '@angular/core';
import { StocksService, OptionDefinition, OptionBreakdown } from '../../services/stocks.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-option-chain',
  templateUrl: './option-chain.component.html',
  styleUrls: ['./option-chain.component.css']
})
export class OptionChainComponent implements OnInit {
  ticker: string;

  public options : OptionDefinition[]
  public filteredOptions : OptionDefinition[]
  public expirationMap : Array<OptionDefinition[]>
  public breakdown : OptionBreakdown
  public stockPrice : number
  public lastUpdated : string
  public expirations: string[]
  public loading : boolean = true

  public expirationSelection : string = ""
  public sideSelection : string = ""
  public minBid : number = 0
  public minStrikePrice : number = 0
  public maxStrikePrice : number = 0

  public failure;

  constructor(
    private service: StocksService,
    private route: ActivatedRoute) { }

  ngOnInit() {
    var ticker = this.route.snapshot.paramMap.get('ticker');
    if (ticker) {
      this.ticker = ticker;
    }

    this.service.getOptionChain(this.ticker).subscribe( result => {
      this.options = result.options
      this.expirations = result.expirations
      this.breakdown = result.breakdown
      this.stockPrice = result.stockPrice
      this.lastUpdated = result.lastUpdated
      this.sideSelection = "put"
      this.minBid = 0.1
      this.runFilter()
    }, error => {
      this.failure = "Failed to load option chain, either data  is not available or entered symbol is incorrect."
			console.log("failed: " + error);
		})
  }

  runFilter() {
    console.log("running filter")
    console.log("expiration: " + this.expirationSelection)
    console.log("side: " + this.sideSelection)
    console.log("min bid: " + this.minBid)
    console.log("min strike price: " + this.minStrikePrice)
    console.log("max strike price: " + this.maxStrikePrice)

    this.filteredOptions = this.options.filter(this.includeOption, this);
    this.loading = false;

    let expirationMap = new Map<string, OptionDefinition[]>();
    this.filteredOptions.forEach(function(value, index, arr) {
      if (!expirationMap.has(value.expirationDate))
      {
        expirationMap.set(value.expirationDate, [value])
      }
      else
      {
          var temp = expirationMap.get(value.expirationDate)
          temp.push(value)
      }
    })
    this.expirationMap = Array.from(expirationMap.values());

    console.log("filter running finished")
  }

  includeOption(element:OptionDefinition, index, array) {
    if (this.expirationSelection !== "") {
      if (element.expirationDate != this.expirationSelection) {
        console.log("filterig out expiration " + element.expirationDate)
        return false
      }
    }

    if (this.sideSelection !== "") {
      if (element.optionType != this.sideSelection) {
        console.log("filterig out side " + element.optionType)
        return false
      }
    }

    if (element.bid < this.minBid) {
      console.log("filtering out min price " + element.bid)
      return false
    }

    if (this.minStrikePrice > 0) {
      if (element.strikePrice < this.minStrikePrice) {
        return false
      }
    }

    if (this.maxStrikePrice > 0) {
      if (element.strikePrice > this.maxStrikePrice) {
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

  onMinBidChange() {
    console.log(this.minBid)
    this.runFilter()
  }

  onMinStrikePriceChange() {
    console.log(this.minStrikePrice)
    this.runFilter()
  }

  onMaxStrikePriceChange() {
    console.log(this.maxStrikePrice)
    this.runFilter()
  }
}
