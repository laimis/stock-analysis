import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { GoogleChartsModule } from 'angular-google-charts';

import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { StockDetailsComponent } from './stock-details/stock-details.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { StockListComponent } from './stock-list/stock-list.component';
import { JobListComponent } from './job-list/job-list.component';
import { StockPurchaseComponent } from './stock-purchase/stock-purchase.component';
import { OptionPurchaseComponent } from './option-purchase/option-purchase.component';

var routes = [
	{ path: '', redirectTo: '/dashboard', pathMatch: 'full' },
	{ path: 'dashboard', component: DashboardComponent},

	{ path: 'stocks/list', component: StockListComponent},
	{ path: 'stocks/purchase', component: StockPurchaseComponent},
	{ path: 'stocks/purchase/:ticker', component: StockPurchaseComponent},
	{ path: 'stocks/:ticker', component: StockDetailsComponent},

	{ path: 'options/purchase', component: OptionPurchaseComponent},

	{ path: 'jobs', component: JobListComponent}
	
]

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
	StockDetailsComponent,
	DashboardComponent,
	StockListComponent,
	JobListComponent,
	StockPurchaseComponent,
	OptionPurchaseComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
	RouterModule.forRoot(routes),
	GoogleChartsModule.forRoot()
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
