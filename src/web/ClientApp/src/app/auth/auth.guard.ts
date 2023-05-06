import { ActivatedRouteSnapshot, Router, RouterStateSnapshot } from '@angular/router';
import { Injectable } from '@angular/core';
import { StocksService, AccountStatus } from '../services/stocks.service';
import { GlobalService } from '../services/global.service';

@Injectable({providedIn: 'root'})
export class AuthGuard  {

	constructor(
    private globalService : GlobalService,
    private stocks : StocksService,
    private router : Router){
	}

  async canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<boolean> {
    //get user data
    const result = await this.stocks.getProfile().toPromise<AccountStatus>();
    if (!result.loggedIn) {
      this.router.navigate(['/profile/login'], { queryParams: { returnUrl: state.url }})
      return false;
    }
    if (!result.verified) {
      this.router.navigate(['/profile/verify'])
      return false;
    }
    this.globalService.markLoggedIn()
    return true;
  }
}

@Injectable({providedIn: 'root'})
export class AuthGuardUnverifiedAllowed  {

  constructor(
    private globalService : GlobalService,
    private stocks : StocksService,
    private router : Router){}

 async canActivate(): Promise<boolean> {
   //get user data
   const result = await this.stocks.getProfile().toPromise<AccountStatus>();
   if (!result.loggedIn) {
     this.router.navigate(['/landing'])
     return false;
   }
   this.globalService.markLoggedIn()
   return true;
 }
}

@Injectable({providedIn: 'root'})
export class AuthGuardAdminOnly  {

  constructor(
    private globalService : GlobalService,
    private stocks : StocksService,
    private router : Router){}

 async canActivate(): Promise<boolean> {
   //get user data
   const result = await this.stocks.getProfile().toPromise<AccountStatus>();
   if (!result.isAdmin) {
     this.router.navigate(['/unauthorized'])
     return false;
   }
   this.globalService.markLoggedIn()
   return true;
 }
}
