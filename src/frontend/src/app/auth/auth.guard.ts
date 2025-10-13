import {ActivatedRouteSnapshot, Router, RouterStateSnapshot} from '@angular/router';
import { Injectable, inject } from '@angular/core';
import {AccountStatus, StocksService} from '../services/stocks.service';
import {GlobalService} from '../services/global.service';

@Injectable({providedIn: 'root'})
export class WithLoginStatus {
    private globalService = inject(GlobalService);
    private stocks = inject(StocksService);


    async canActivate(): Promise<boolean> {
        //get user data
        const result = await this.stocks.getProfile().toPromise();
        this.globalService.markLoggedIn(result)
        return true;
    }
}

@Injectable({providedIn: 'root'})
export class AuthGuard {
    private globalService = inject(GlobalService);
    private stocks = inject(StocksService);
    private router = inject(Router);


    async canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<boolean> {
        //get user data
        const result = await this.stocks.getProfile().toPromise();
        if (!result.loggedIn) {
            this.router.navigate(['/profile/login'], {queryParams: {returnUrl: state.url}})
            return false;
        }
        if (!result.verified) {
            this.router.navigate(['/profile/verify'])
            return false;
        }
        this.globalService.markLoggedIn(result)
        return true;
    }
}

@Injectable({providedIn: 'root'})
export class AuthGuardUnverifiedAllowed {
    private globalService = inject(GlobalService);
    private stocks = inject(StocksService);
    private router = inject(Router);


    async canActivate(): Promise<boolean> {
        //get user data
        const result = await this.stocks.getProfile().toPromise();
        if (!result.loggedIn) {
            this.router.navigate(['/landing'])
            return false;
        }
        this.globalService.markLoggedIn(result)
        return true;
    }
}

@Injectable({providedIn: 'root'})
export class AuthGuardAdminOnly {
    private globalService = inject(GlobalService);
    private stocks = inject(StocksService);
    private router = inject(Router);


    async canActivate(): Promise<boolean> {
        //get user data
        const result = await this.stocks.getProfile().toPromise();
        if (!result.isAdmin) {
            this.router.navigate(['/unauthorized'])
            return false;
        }
        this.globalService.markLoggedIn(result)
        return true;
    }
}
