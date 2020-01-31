import { Component, OnInit } from '@angular/core';
import { StocksService, GetErrors } from '../services/stocks.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-profile-login',
  templateUrl: './profile-login.component.html',
  styleUrls: ['./profile-login.component.css']
})
export class ProfileLoginComponent implements OnInit {

  public email      :string
  public password   :string

  public errors     :string[]

  public resetPasswordRequest : boolean
  public passwordRequestSuccess : boolean

  constructor(
    private stockService : StocksService,
    private router: Router) { }

  ngOnInit() {}

  login() {

    this.errors = null

    var obj = {
      email: this.email,
      password: this.password
    }

    this.stockService.loginAccount(obj).subscribe(r => {
      this.router.navigate(['/dashboard'])
    }, err => {
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
}
