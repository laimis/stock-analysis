import { CanActivate } from '@angular/router';
import { Injectable, Inject } from '@angular/core';
import { StocksService, AccountStatus } from '../services/stocks.service';

@Injectable({providedIn: 'root'})
export class AuthGuard implements CanActivate {

	 window : Window;

	constructor(private stocks : StocksService, @Inject("windowObject") $window: Window){
		this.window = $window
	}

  async canActivate(): Promise<boolean> {
    //get user data
    const result = await this.stocks.getAccountStatus().toPromise<AccountStatus>();
    if (!result.loggedIn) {
      this.window.location.href = "/api/account/login";
      return false;
    }
    return true;
  }
}
