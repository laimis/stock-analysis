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
}
