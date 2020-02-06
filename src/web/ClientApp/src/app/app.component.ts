import { Component } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';

declare let ga:Function;

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'app';

  constructor(router: Router){
    router.events.subscribe((y: NavigationEnd) => {
      if(y instanceof NavigationEnd){
        ga('set', 'page', y.urlAfterRedirects)
        ga('send', 'pageview');
      }
    })
  }
}
