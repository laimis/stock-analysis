import { Component, OnInit } from '@angular/core';
import { StocksService, GetErrors } from '../services/stocks.service';

@Component({
  selector: 'app-payments',
  templateUrl: './payments.component.html',
  styleUrls: ['./payments.component.css']
})
export class PaymentsComponent implements OnInit {

  success: Boolean = false
  errors: string[]

  constructor(private stockService : StocksService) { }

  ngOnInit() {
  }

  payFull() {
    this.pay("plan_GmXCy9dpWKIB4E", "Plan level: Full")
  }

  payStarter() {
    this.pay("plan_GmXCWQvmEWwhtr", "Plan level: Starter")
  }

  pay(planId:string, planName:string) {

    this.errors = null
    this.success = false;

    var service = this.stockService;
    var capture = this;

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
          () => capture.success = true,
          err => capture.errors = GetErrors(err))
      }
    });

    handler.open({
      name: 'Nightingale Trading',
      description: planName
    });
}

}
