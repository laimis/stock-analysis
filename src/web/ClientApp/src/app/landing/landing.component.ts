import { Component, OnInit } from '@angular/core';
import { StocksService, GetErrors } from '../services/stocks.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-landing',
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.css']
})
export class LandingComponent implements OnInit {

  success: Boolean = false
  errors: string[]

  constructor(
    private stockService : StocksService,
    private router:Router) { }

  ngOnInit() {
  }

  payFull() {
    this.pay("plan_GmXCy9dpWKIB4E", "Plan level: Full")
  }

  payStarter() {
    this.pay("plan_GmXCWQvmEWwhtr", "Plan level: Plus")
  }

  pay(planId:string, planName:string) {

    this.errors = null
    this.success = false;

    var service = this.stockService;
    var capture = this;
    var router = this.router;

    var handler = (<any>window).StripeCheckout.configure({
      key: 'pk_test_XKCVJk24i9R0N8He0YkHC7uA00trzQsSKK',
      locale: 'auto',
      token: function (token: any) {
        console.log(token)

        var obj = {
          token : token,
          planId : planId
        }

        service.createSubsription(obj).subscribe(
          () => router.navigateByUrl("/dashboard"),
          err => {
            capture.errors = GetErrors(err)
          }
        )
      }
    });

    handler.open({
      name: 'Nightingale Trading',
      description: planName
    });
  }
}
