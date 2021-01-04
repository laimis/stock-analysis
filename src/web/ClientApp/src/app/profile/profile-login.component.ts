import { Component, OnInit } from '@angular/core';
import { StocksService, GetErrors } from '../services/stocks.service';
import { Router } from '@angular/router';
import { Location } from '@angular/common';

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

  constructor(
    private stockService  : StocksService,
    private router        : Router,
    private location      : Location) { }

  ngOnInit() {}

  login() {

    this.errors = null
    this.inProgress = true

    var obj = {
      email: this.email,
      password: this.password
    }

    this.stockService.loginAccount(obj).subscribe(_ => {
      this.router.navigate(['/dashboard'])
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

  forgotPassword() {
    this.resetPasswordRequest = true
  }

  cancelReset() {
    this.resetPasswordRequest = false
  }

  back() {
    this.location.back()
  }
}
