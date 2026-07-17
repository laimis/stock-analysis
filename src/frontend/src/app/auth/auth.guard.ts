import {ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree} from '@angular/router';
import { Injectable, inject } from '@angular/core';
import {StocksService} from '../services/stocks.service';
import {GlobalService} from '../services/global.service';
import { firstValueFrom } from 'rxjs';

@Injectable({providedIn: 'root'})
export class WithLoginStatus {
    private globalService = inject(GlobalService);
    private stocks = inject(StocksService);


    async canActivate(): Promise<boolean> {
        //get user data
        const result = await firstValueFrom(this.stocks.getProfile());
        this.globalService.markLoggedIn(result)
        return true;
    }
}

@Injectable({providedIn: 'root'})
export class AuthGuard {
    private globalService = inject(GlobalService);
    private stocks = inject(StocksService);
    private router = inject(Router);


    async canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<boolean | UrlTree> {
        try {
            const result = await firstValueFrom(this.stocks.getProfile());
            if (!result.loggedIn) {
                return this.router.createUrlTree(['/profile/login'], {queryParams: {returnUrl: state.url}});
            }
            if (!result.verified) {
                return this.router.createUrlTree(['/profile/verify']);
            }
            this.globalService.markLoggedIn(result)
            return true;
        } catch {
            return this.router.createUrlTree(['/profile/login'], {queryParams: {returnUrl: state.url}});
        }
    }
}

@Injectable({providedIn: 'root'})
export class AuthGuardUnverifiedAllowed {
    private globalService = inject(GlobalService);
    private stocks = inject(StocksService);
    private router = inject(Router);


    async canActivate(): Promise<boolean | UrlTree> {
        try {
            const result = await firstValueFrom(this.stocks.getProfile());
            if (!result.loggedIn) {
                return this.router.createUrlTree(['/landing']);
            }
            this.globalService.markLoggedIn(result)
            return true;
        } catch {
            return this.router.createUrlTree(['/landing']);
        }
    }
}

@Injectable({providedIn: 'root'})
export class AuthGuardAdminOnly {
    private globalService = inject(GlobalService);
    private stocks = inject(StocksService);
    private router = inject(Router);


    async canActivate(): Promise<boolean | UrlTree> {
        try {
            const result = await firstValueFrom(this.stocks.getProfile());
            if (!result.isAdmin) {
                return this.router.createUrlTree(['/unauthorized']);
            }
            this.globalService.markLoggedIn(result)
            return true;
        } catch {
            return this.router.createUrlTree(['/unauthorized']);
        }
    }
}
