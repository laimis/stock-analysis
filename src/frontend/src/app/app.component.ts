import {Component} from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { NavMenuComponent } from "./nav-menu/nav-menu.component";
import { VERSION_INFO } from './version.generated';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css'],
    imports: [RouterLink, RouterOutlet, NavMenuComponent]
})
export class AppComponent {
    title = 'app';
    year = new Date().getFullYear();
    version = VERSION_INFO.version;
    buildDate = VERSION_INFO.formattedBuildDate;
}
