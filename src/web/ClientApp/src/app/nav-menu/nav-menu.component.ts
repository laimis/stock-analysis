import { Component } from '@angular/core';
import { GlobalService } from '../services/global.service';

@Component({
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.css']
})
export class NavMenuComponent {
  isLoggedIn = false

  links = [
    { path: '/trading', label: 'Active Trades' },
    { path: '/stocks/newposition', label: 'New Position' },
    { path: '/trading/dailyanalysis', label: 'Daily Analysis' },
    { path: '/stocks/lists', label: 'Stock Lists' },
    { path: '/summary', label: 'Weekly Summary' },
    { path: '/profile', label: 'Profile'}
  ];

  constructor(
    public globalService: GlobalService
  ) {
    this.globalService.customVariable.subscribe((value) => {
      console.log("nav menu got new value: ")
      console.log(value)
      this.isLoggedIn = value.isLoggedIn;
    }); 
  }
}
