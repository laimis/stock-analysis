import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';
import { Router, ActivatedRoute } from '@angular/router';
import { Location } from '@angular/common';
import { GetErrors } from '../services/utils';

@Component({
  selector: 'app-profile-create',
  templateUrl: './profile-create.component.html',
  styleUrls: ['./profile-create.component.css']
})
export class ProfileCreateComponent implements OnInit {

  firstname  : string
  lastname   : string
  email      : string
  password   : string
  terms      : boolean

  errors     : string[]

  disableButton : boolean = false

  private FREE_PLAN  : string = 'FREE'
  private PLUS_PLAN  : string = 'PLUS'
  private FULL_PLAN  : string = 'FULL'

  planName   : string = this.FREE_PLAN
  isPremium  : boolean = false
  payLabel   : string = ''

  constructor(
    private stockService  : StocksService,
    private router        : Router,
    private location      : Location,
    private route   : ActivatedRoute) { }

  ngOnInit() {
    var plan = this.route.snapshot.paramMap.get("plan")
    if (plan)
    {
      if (plan == 'plus') this.planName = this.PLUS_PLAN
      if (plan == 'full') this.planName = this.FULL_PLAN

      this.isPremium = true;
      this.payLabel = " & Pay"
    }
  }

  validate() {

    this.errors = null
    this.disableButton = true

    var obj = this.createUserData()

    this.stockService.validateAccount(obj).subscribe(r => {
      if (this.planName == this.PLUS_PLAN) {
        this.payPlus()
      } else if (this.planName == this.FULL_PLAN) {
        this.payFull()
      } else {
        this.createAccount(obj, null)
      }
    }, err => {
      this.disableButton = false
      this.errors = GetErrors(err)
    })
  }

  createAccount(userData, paymentToken){
    var obj = {
      userInfo: userData,
      paymentInfo: paymentToken
    }

    this.stockService.createAccount(obj).subscribe(_ => {
      this.router.navigate(['/dashboard'])
      this.disableButton = false
    }, err => {
      this.errors = GetErrors(err)
      this.disableButton = false
    })
  }

  private createUserData() {
    return {
      firstname: this.firstname,
      lastname: this.lastname,
      email: this.email,
      password: this.password,
      terms: this.terms
    };
  }

  back() {
    this.location.back()
  }

  payFull() {
    this.pay("plan_GmXCy9dpWKIB4E", "Plan level: Full")
  }

  payPlus() {
    this.pay("plan_GmXCWQvmEWwhtr", "Plan level: Plus")
  }

  pay(planId:string, planName:string) {

    var capture = this;

    var handler = (<any>window).StripeCheckout.configure({
      key: 'pk_live_dls5NvmF6iwb4W19DlqsYvYR0006lBNU20',
      locale: 'auto',
      token: function (token: any) {
        console.log(token)

        var paymentData = {
          token : token,
          planId : planId
        }

        capture.createAccount(
          capture.createUserData(),
          paymentData)
      }
    });

    handler.open({
      name: 'Nightingale Trading',
      description: planName
    });

    this.disableButton = false
  }
}
