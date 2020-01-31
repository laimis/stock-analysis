import { Component, OnInit } from '@angular/core';
import { StocksService, GetErrors } from '../services/stocks.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-profile-create',
  templateUrl: './profile-create.component.html',
  styleUrls: ['./profile-create.component.css']
})
export class ProfileCreateComponent implements OnInit {

  public firstname  :string
  public lastname   :string
  public email      :string
  public password   :string

  public errors     :string[]

  constructor(
    private stockService : StocksService,
    private router: Router) { }

  ngOnInit() {}

  create() {

    this.errors = null

    var obj = {
      firstname: this.firstname,
      lastname: this.lastname,
      email: this.email,
      password: this.password
    }

    this.stockService.createAccount(obj).subscribe(r => {
      this.router.navigate(['/dashboard'])
    }, err => {
      this.errors = GetErrors(err)
    })
  }
}
