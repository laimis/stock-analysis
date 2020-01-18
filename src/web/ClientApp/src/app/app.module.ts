
import { AdminComponent } from './admin/admin.component';
import { AppComponent } from './app.component';
import { AuthGuard } from './auth/auth.guard';
import { BrowserModule } from '@angular/platform-browser';
import { DashboardComponent } from './dashboard/dashboard.component';
import { FormsModule } from '@angular/forms';
import { GoogleChartsModule } from 'angular-google-charts';
import { HttpClientModule } from '@angular/common/http';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { NgModule } from '@angular/core';
import { OptionDetailComponent } from './option-detail/option-detail.component';
import { OptionSellComponent } from './option-sell/option-sell.component';
import { ProfileComponent } from './profile/profile.component';
import { RouterModule } from '@angular/router';
import { SoldOptionDetailComponent } from './sold-option-detail/sold-option-detail.component';
import { StockDetailsComponent } from './stock-details/stock-details.component';
import { StockPurchaseComponent } from './stock-purchase/stock-purchase.component';
import { StockListsComponent } from './stock-lists/stock-lists.component';
import { StockListComponent } from './stock-lists/stock-list.component';

var routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full', canActivate: [AuthGuard] },
  { path: 'admin', component: AdminComponent},
  { path: 'dashboard', component: DashboardComponent, canActivate: [AuthGuard] },
  { path: 'profile', component: ProfileComponent},
  { path: 'options/sell', component: OptionSellComponent },
  { path: 'options/:ticker', component: OptionDetailComponent },
  { path: 'soldoptions/:ticker/:type/:strikePrice/:expiration', component: SoldOptionDetailComponent},
  { path: 'stocks/lists', component: StockListsComponent },
  { path: 'stocks/purchase', component: StockPurchaseComponent },
  { path: 'stocks/purchase/:ticker', component: StockPurchaseComponent },
  { path: 'stocks/:ticker', component: StockDetailsComponent },
]

@NgModule({
  declarations: [
    AdminComponent,
    AppComponent,
    DashboardComponent,
    NavMenuComponent,
    OptionDetailComponent,
    OptionSellComponent,
    ProfileComponent,
    SoldOptionDetailComponent,
    StockDetailsComponent,
    StockListsComponent,
    StockListComponent,
    StockPurchaseComponent,
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
