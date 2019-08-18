import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { StockDetailsComponent } from './stock-details/stock-details.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { StockListComponent } from './stock-list/stock-list.component';

var routes = [
	{ path: '', redirectTo: '/dashboard', pathMatch: 'full' },
	{ path: 'dashboard', component: DashboardComponent},

	{ path: 'stocks/list', component: StockListComponent},
	{ path: 'stocks/:ticker', component: StockDetailsComponent},
	
]

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
	StockDetailsComponent,
	DashboardComponent,
	StockListComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    RouterModule.forRoot(routes)
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
