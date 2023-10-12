import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import {AccountStatus} from "./stocks.service";

@Injectable({
  providedIn: 'root'
})
export class GlobalService {
  public accountStatusFeed = new BehaviorSubject<AccountStatus>(new AccountStatus());

  markLoggedIn(accountStatus:AccountStatus) {
    this.accountStatusFeed.next(accountStatus)
  }
}

