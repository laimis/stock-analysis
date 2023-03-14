import { Component } from '@angular/core';
import { NavigationEnd, Router, RouterEvent } from '@angular/router';
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
    { path: '/trading/analysis', label: 'Analysis' },
    { path: '/trading/review', label: 'Review'},
    { path: '/summary', label: 'Weekly Summary' },
    { path: '/stocks/lists', label: 'Stock Lists' },
    { path: '/profile', label: 'Profile'}
  ];
  currentPath: string;

  constructor(
    public globalService: GlobalService,
    private router: Router
  ) {
    this.globalService.customVariable.subscribe((value) => {
      this.isLoggedIn = value.isLoggedIn;
    });

    this.router.events.subscribe((val:RouterEvent) => {
      if (val instanceof NavigationEnd) {
        // Hide loading indicator
        this.currentPath = val.url;
      }
    })
  }
}
