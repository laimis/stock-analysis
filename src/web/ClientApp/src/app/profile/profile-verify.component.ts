import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';
import { Router, ActivatedRoute } from '@angular/router';
import { GetErrors } from '../services/utils';

@Component({
  selector: 'app-profile-verify',
  templateUrl: './profile-verify.component.html',
  styleUrls: ['./profile-verify.component.css']
})
export class ProfileVerifyComponent implements OnInit {

  public errors     :string[]
  public id         :string
  public password   :string

  constructor(
    private stockService : StocksService,
    private route: ActivatedRoute,
    private router: Router) { }

  ngOnInit() {
    this.id = this.route.snapshot.paramMap.get("id")
  }

  resetPassword() {

    this.errors = null

    var obj = {
      id: this.id,
      password: this.password
    }

    this.stockService.resetPassword(obj).subscribe(r => {
      this.router.navigate(['/dashboard'])
    }, err => {
      this.errors = GetErrors(err)
    })
  }
}
