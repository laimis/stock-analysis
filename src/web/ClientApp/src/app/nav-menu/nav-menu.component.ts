import { Component } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { GlobalService } from '../services/global.service';

@Component({
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.css']
})
export class NavMenuComponent {
  isLoggedIn = false

  links = [
    { path: '/trading', label: 'Positions' },
    { path: '/stocks/newposition', label: 'Trade' },
    { path: '/trading/analysis', label: 'Daily Analysis' },
    { path: '/routines', label: 'Routines' },
    { path: '/profile', label: 'Profile'}
  ];
  currentPath: string;

  constructor(
    public globalService: GlobalService,
    private router: Router
  ) {
    this.globalService.accountStatusFeed.subscribe((value) => {
      this.isLoggedIn = value.loggedIn;
    });

    this.router.events.subscribe((val) => {
      if (val instanceof NavigationEnd) {
        this.currentPath = val.url;
      }
    })
  }
}
