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
import { OptionSellComponent } from './option-sell/option-sell.component';
import { OptionDetailComponent } from './option-detail/option-detail.component';
import { SoldOptionDetailComponent } from './sold-option-detail/sold-option-detail.component';
import { AuthGuard } from './auth/auth.guard';

var routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full', canActivate: [AuthGuard] },
  { path: 'dashboard', component: DashboardComponent, canActivate: [AuthGuard] },

  { path: 'stocks/list', component: StockListComponent },
  { path: 'stocks/purchase', component: StockPurchaseComponent },
  { path: 'stocks/purchase/:ticker', component: StockPurchaseComponent },
  { path: 'stocks/:ticker', component: StockDetailsComponent },

  { path: 'options/sell', component: OptionSellComponent },
  { path: 'options/:ticker', component: OptionDetailComponent },

  { path: 'soldoptions/:ticker/:type/:strikePrice/:expiration', component: SoldOptionDetailComponent},

  { path: 'jobs', component: JobListComponent }

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
    OptionSellComponent,
    OptionDetailComponent,
    SoldOptionDetailComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    RouterModule.forRoot(routes),
    GoogleChartsModule.forRoot()
  ],
  providers: [
	  { provide: "windowObject", useValue: window}
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
