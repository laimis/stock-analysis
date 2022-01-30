import { Component, OnInit } from '@angular/core';
import { job, stocktransactioncommand, StocksService } from '../services/stocks.service';

@Component({
  selector: 'app-playground',
  templateUrl: './playground.component.html',
  styleUrls: ['./playground.component.css']
})

export class PlaygroundComponent implements OnInit {

  enteredTicker : string
  result : any

  jobs : job[] = new Array()
  job : job

  parsed : boolean = false
  processed : boolean = false

  constructor(private stocks:StocksService) { }

  ngOnInit() {
  }

  fetch(ticker:string) {
    console.log(ticker)
    this.enteredTicker = ticker

    this.stocks.registerForTracking(ticker).subscribe(r => {
      this.result = r
    })
  }

  parse(input:string) {
    input.split(/\r?\n/).reverse().forEach( (v, i, a) => {
      if (v.indexOf("TRANSACTION ID") > 0) {
        return
      }

      // "3/26/2021	33741400972	Bought 6 CRWD @ 170.902"
      var line = v.split('	')
      if (line.length != 3) {
        return
      }

      // 	Bought 6 CRWD @ 170.902
      var details = line[2].split(' ')

      if (details.length != 5) {
        return
      }

      var j = new job()
      j.date = new Date(line[0]).toISOString()
      j.id = line[1]

      j.bought = details[0] == "Bought"
      j.sold = details[0] == "Sold"

      if (j.sold == false && j.bought == false) {
        return
      }

      j.amount = Number.parseInt(details[1])
      j.ticker = details[2]
      j.price = Number.parseFloat(details[4])

      this.jobs = this.jobs.concat(j)
    })

    this.parsed = true

    this.process()
  }

  process() {
    if (this.jobs.length == 0) {
      console.log('finished')
      this.processed = true
      this.job = null
      return
    }

    this.job = this.jobs[0]

    console.log('processing ' + this.job.id)

    this.processjob()
  }

  processjob() {

    if (this.job.bought) {
      var cmd = new stocktransactioncommand()
      cmd.date = this.job.date
      cmd.numberOfShares = this.job.amount
      cmd.price = this.job.price
      cmd.ticker = this.job.ticker

      this.stocks.purchase(cmd).subscribe(_ => {
        console.log("bought " + this.job)
        this.jobs.shift()
        this.process()
      }, err => {
        console.log("buy error: " + err + ", " + this.job)
        this.jobs.shift()
        this.process()
      })
    }

    if (this.job.sold) {
      var cmd = new stocktransactioncommand()
      cmd.date = this.job.date
      cmd.numberOfShares = this.job.amount
      cmd.price = this.job.price
      cmd.ticker = this.job.ticker

      this.stocks.sell(cmd).subscribe(_ => {
        console.log("sold " + this.job)
        this.jobs.shift()
        this.process()
      }, err => {
        console.log("sell error: " + err + ", " + this.job)
        this.jobs.shift()
        this.process()
      })
    }
  }

}
