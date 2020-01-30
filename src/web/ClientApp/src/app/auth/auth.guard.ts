import { CanActivate, Router } from '@angular/router';
import { Injectable, Inject } from '@angular/core';
import { StocksService, AccountStatus } from '../services/stocks.service';

@Injectable({providedIn: 'root'})
export class AuthGuard implements CanActivate {

	 window : Window;

	constructor(
    private stocks : StocksService,
    @Inject("windowObject") $window: Window,
    private router : Router){
		this.window = $window
	}

  async canActivate(): Promise<boolean> {
    //get user data
    const result = await this.stocks.getAccountStatus().toPromise<AccountStatus>();
    if (!result.loggedIn) {
      this.router.navigate(['/landing'])
      return false;
    }
    return true;
  }
}
