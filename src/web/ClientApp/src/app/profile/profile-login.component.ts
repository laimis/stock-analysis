import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';
import { ActivatedRoute, Router } from '@angular/router';
import { Location } from '@angular/common';
import { GetErrors } from '../services/utils';
import { GlobalService } from '../services/global.service';

@Component({
  selector: 'app-profile-login',
  templateUrl: './profile-login.component.html',
  styleUrls: ['./profile-login.component.css']
})
export class ProfileLoginComponent implements OnInit {

  public email      :string
  public password   :string

  public errors     :string[]

  inProgress : boolean

  public resetPasswordRequest : boolean
  public passwordRequestSuccess : boolean
  returnUrl: string;

  constructor(
    private stockService  : StocksService,
    private route         : ActivatedRoute,
    private router        : Router,
    private location      : Location,
    private globalService : GlobalService) { }

  ngOnInit() {
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/dashboard';
  }

  login() {

    this.errors = null
    this.inProgress = true

    var obj = {
      email: this.email,
      password: this.password
    }

    this.stockService.loginAccount(obj).subscribe(_ => {
      console.log("logged in setting global variable")
      this.globalService.markLoggedIn()
      this.router.navigateByUrl(this.returnUrl)
    }, err => {
      this.inProgress = false
      this.errors = GetErrors(err)
    })
  }

  requestReset() {

    this.errors = null
    this.passwordRequestSuccess = false

    var obj = {
      email: this.email
    }

    this.stockService.requestPasswordReset(obj).subscribe(r => {
      this.email = null
      this.passwordRequestSuccess = true
    }, err => {
      this.errors = GetErrors(err)
    })
  }

  forgotPassword() : boolean {
    this.resetPasswordRequest = true
    return false
  }

  cancelReset() {
    this.resetPasswordRequest = false
  }

  back() {
    this.location.back()
  }
}
