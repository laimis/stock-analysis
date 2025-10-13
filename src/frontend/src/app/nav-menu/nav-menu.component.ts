import { Component, inject } from '@angular/core';
import {NavigationEnd, Router} from '@angular/router';
import {GlobalService} from '../services/global.service';
import {GetErrors} from "../services/utils";

@Component({
    selector: 'app-nav-menu',
    templateUrl: './nav-menu.component.html',
    styleUrls: ['./nav-menu.component.css'],
    standalone: false
})
export class NavMenuComponent {
    globalService = inject(GlobalService);
    private router = inject(Router);

    isLoggedIn = false
    errors: string[] = null

    links = [
        {path: '/stocks/positions', label: 'Stocks'},
        {path: '/options', label: 'Options'},
        {path: '/stocks/newposition', label: 'Trade'},
        {path: '/routines', label: 'Routines'},
        {path: '/profile', label: 'Profile'}
    ];
    currentPath: string;
    // doesn't collapse automatically
    navbarSupportedContentId = 'navbarSupportedContent';

    // on mobile, when clicking a link, collapse the menu, it

    constructor() {
        this.globalService.accountStatusFeed.subscribe(
            (value) => {
                this.isLoggedIn = value.loggedIn;
            },
            (error) => {
                this.errors = GetErrors(error);
            }
        );

        this.router.events.subscribe((val) => {
            if (val instanceof NavigationEnd) {
                this.currentPath = val.url;
            }
        }, (error) => {
            this.errors = GetErrors(error);
        })
    }

    collapseMenu() {
        let element = document.getElementById(this.navbarSupportedContentId);
        element.classList.remove('show');
    }

    navigateToTicker(ticker: string) {
        this.router.navigate(['/stocks/' + ticker]).then(
            result => {
                this.collapseMenu();
            }
        )
    }
}
