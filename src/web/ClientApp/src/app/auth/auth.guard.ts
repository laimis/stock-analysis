import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot } from '@angular/router';
import { Injectable } from '@angular/core';
import { StocksService, AccountStatus } from '../services/stocks.service';

@Injectable({providedIn: 'root'})
export class AuthGuard implements CanActivate {

	constructor(
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
    return true;
  }
}

@Injectable({providedIn: 'root'})
export class AuthGuardUnverifiedAllowed implements CanActivate {

  constructor(
    private stocks : StocksService,
    private router : Router){}

 async canActivate(): Promise<boolean> {
   //get user data
   const result = await this.stocks.getProfile().toPromise<AccountStatus>();
   if (!result.loggedIn) {
     this.router.navigate(['/landing'])
     return false;
   }
   return true;
 }
}

@Injectable({providedIn: 'root'})
export class AuthGuardAdminOnly implements CanActivate {

  constructor(
    private stocks : StocksService,
    private router : Router){}

 async canActivate(): Promise<boolean> {
   //get user data
   const result = await this.stocks.getProfile().toPromise<AccountStatus>();
   if (!result.isAdmin) {
     this.router.navigate(['/unauthorized'])
     return false;
   }
   return true;
 }
}
