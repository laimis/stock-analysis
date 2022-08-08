
import { AddNoteComponent } from './notes/add-note.component';
import { AppComponent } from './app.component';
import { AuthGuard, AuthGuardUnverifiedAllowed, AuthGuardAdminOnly } from './auth/auth.guard';
import { BrowserModule, Title } from '@angular/platform-browser';
import { DashboardComponent } from './dashboard/dashboard.component';
import { ErrorDisplayComponent } from './shared/error-display/error-display.component';
import { EventsComponent } from './events/events.component';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { LandingComponent } from './landing/landing.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { NgModule } from '@angular/core';
import { NoteComponent } from './notes/note.component';
import { NotesComponent } from './notes/notes.component';
import { OptionChainComponent } from './options/option-chain/option-chain.component';
import { OptionSellComponent } from './options/option-sell/option-sell.component';
import { ProfileComponent } from './profile/profile.component';
import { ProfileCreateComponent } from './profile/profile-create.component';
import { ProfileLoginComponent } from './profile/profile-login.component';
import { SummaryComponent } from './summary/summary.component';
import { RouterModule, Routes } from '@angular/router';
import { OwnedOptionComponent } from './options/owned-option-detail/owned-option-detail.component';
import { StockDetailsComponent } from './stocks/stock-details/stock-details.component';
import { TransactionsComponent } from './transactions/transactions.component';
import { ProfilePasswordResetComponent } from './profile/profile-passwordreset.component';
import { ProfileVerifyComponent } from './profile/profile-verify.component';
import { ContactComponent } from './contact/contact.component';
import { TermsComponent } from './terms/terms.component';
import { PrivacyComponent } from './privacy/privacy.component';
import { SymbolSearchComponent } from './symbol-search/symbol-search.component';
import { PlaygroundComponent } from './playground/playground.component';
import { AdminEmailComponent } from './admin/email/admin-email.component';
import { AdminWeeklyComponent } from './admin/weekly/admin-weekly.component';
import { PaymentsComponent } from './payments/payments.component';
import { StockFundamentalsComponent } from './stocks/stock-details/stock-fundamentals.component';
import { StockNotesComponent } from './stocks/stock-details/stock-notes.component';
import { StockOwnershipComponent } from './stocks/stock-details/stock-ownership.component';
import { StockOptionComponent } from './stocks/stock-details/stock-option.component';
import { StockGridComponent } from './stocks/stock-dashboard/stock-grid.component';
import { StockAlertsComponent } from './stocks/stock-details/stock-alerts.component';
import { AlertsComponent } from './alerts/alerts.component';
import { AdminUsersComponent } from './admin/users/admin-users.component';
import { OptionsComponent } from './options/option-dashboard/option-dashboard.component';
import { OptionStatsComponent } from './options/option-dashboard/option-stats.component';
import { OptionOpenComponent } from './options/option-dashboard/option-open.component';
import { OptionPerformanceComponent } from './options/option-dashboard/option-performance.component';
import { OptionClosedComponent } from './options/option-dashboard/option-closed.component';
import { StockDashboardComponent } from './stocks/stock-dashboard/stock-dashboard.component';
import { StockOwnershipGridComponent } from './stocks/stock-dashboard/stock-ownership-grid.component';
import { FailuresuccesschainComponent } from './reports/failuresuccesschain/failuresuccesschain/failuresuccesschain.component';
import { RecentSellsComponent } from './recentsells/recentsells.component';
import { CryptoDashboardComponent } from './cryptos/crypto-dashboard/crypto-dashboard.component';
import { CryptoOwnershipGridComponent } from './cryptos/crypto-dashboard/crypto-ownership-grid.component';
import { CryptoDetailsComponent } from './cryptos/crypto-details/crypto-details.component';
import { NgChartsModule } from 'ng2-charts';
import { StockTradingComponent } from './stocks/stock-trading/stock-trading-dashboard.component';
import { StockTradingPositionComponent } from './stocks/stock-trading/stock-trading-positions.component';
import { StockTradingNewPositionComponent } from './stocks/stock-trading/stock-trading-newposition.component';
import { StockTradingPastComponent } from './stocks/stock-trading/stock-trading-past.component';
import { StockTradingPerformanceComponent } from './stocks/stock-trading/stock-trading-performance.component';
import { StockTradingReviewComponent } from './stocks/stock-trading/stock-trading-review.component';
import { StockTradingChartComponent } from './stocks/stock-trading/stock-trading-chart.component';
import { StockChartsComponent } from './stocks/stock-dashboard/stock-charts.component';
import { StockTradingPendingComponent } from './stocks/stock-trading/stock-trading-brokerage.component';
import { StockViolationsComponent } from './stocks/stock-dashboard/stock-violations.component';

var routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'alerts', component: AlertsComponent, canActivate: [AuthGuard] },
  { path: 'admin/email', component: AdminEmailComponent, canActivate: [AuthGuardAdminOnly] },
  { path: 'admin/users', component: AdminUsersComponent, canActivate: [AuthGuardAdminOnly] },
  { path: 'admin/weekly', component: AdminWeeklyComponent, canActivate: [AuthGuardAdminOnly] },
  { path: 'contact', component: ContactComponent },
  { path: 'dashboard', component: DashboardComponent, canActivate: [AuthGuard] },
  { path: 'events', component: EventsComponent, canActivate: [AuthGuard]},
  { path: 'landing', component: LandingComponent },
  { path: 'profile', component: ProfileComponent, canActivate: [AuthGuardUnverifiedAllowed]},
  { path: 'profile/create', component: ProfileCreateComponent },
  { path: 'profile/create/:plan', component: ProfileCreateComponent },
  { path: 'profile/login', component: ProfileLoginComponent },
  { path: 'profile/verify', component: ProfileVerifyComponent },
  { path: 'profile/passwordreset/:id', component: ProfilePasswordResetComponent },
  { path: 'notes', component: NotesComponent, canActivate: [AuthGuard]},
  { path: 'notes/add', component: AddNoteComponent, canActivate: [AuthGuard]},
  { path: 'notes/add/:ticker', component: AddNoteComponent, canActivate: [AuthGuard]},
  { path: 'notes/filtered/:ticker', component: NotesComponent, canActivate: [AuthGuard]},
  { path: 'notes/:id', component: NoteComponent, canActivate: [AuthGuard]},

  { path: 'options', component: OptionsComponent, canActivate: [AuthGuard] },
  { path: 'options/sell', component: OptionSellComponent, canActivate: [AuthGuard] },
  { path: 'options/sell/:ticker', component: OptionSellComponent, canActivate: [AuthGuard] },
  { path: 'options/chain/:ticker', component: OptionChainComponent, canActivate: [AuthGuard] },
  { path: 'optiondetails/:id', component: OwnedOptionComponent, canActivate: [AuthGuard]},

  { path: 'summary', component: SummaryComponent, canActivate: [AuthGuard] },
  { path: 'payments', component: PaymentsComponent, canActivate: [AuthGuard]},
  { path: 'playground', component: PlaygroundComponent},
  { path: 'privacy', component: PrivacyComponent},
  { path: 'trading', component: StockTradingComponent},

  { path: 'stocks', component: StockDashboardComponent},
  { path: 'stocks/:ticker', component: StockDetailsComponent, canActivate: [AuthGuard] },

  { path: 'transactions', component: TransactionsComponent, canActivate: [AuthGuard] },
  { path: 'terms', component: TermsComponent},
  { path: 'test', component: SymbolSearchComponent},

  { path: 'reports/chain', component: FailuresuccesschainComponent, canActivate: [AuthGuard]},
  { path: 'reports/recentsells', component: RecentSellsComponent, canActivate: [AuthGuard]},

  { path: 'cryptos', component: CryptoDashboardComponent, canActivate: [AuthGuard]},
  { path: 'cryptos/:token', component: CryptoDetailsComponent, canActivate: [AuthGuard]},
]

@NgModule({
  declarations: [
    AlertsComponent,
    AddNoteComponent,
    AdminEmailComponent,
    AdminWeeklyComponent,
    AdminUsersComponent,
    AppComponent,
    ContactComponent,
    DashboardComponent,
    ErrorDisplayComponent,
    EventsComponent,
    LandingComponent,
    NavMenuComponent,
    NoteComponent,
    NotesComponent,

    OptionsComponent,
    OptionStatsComponent,
    OptionChainComponent,
    OptionSellComponent,
    OptionOpenComponent,
    OptionPerformanceComponent,
    OptionClosedComponent,

    PaymentsComponent,
    PlaygroundComponent,
    PrivacyComponent,
    ProfileComponent,
    ProfileCreateComponent,
    ProfileLoginComponent,
    ProfilePasswordResetComponent,
    ProfileVerifyComponent,
    SummaryComponent,
    OwnedOptionComponent,

    StockAlertsComponent,
    StockDashboardComponent,
    StockDetailsComponent,
    StockFundamentalsComponent,
    StockGridComponent,
    StockNotesComponent,
    StockOptionComponent,
    StockOwnershipComponent,
    StockOwnershipGridComponent,
    StockChartsComponent,
    StockViolationsComponent,
    
    StockTradingComponent,
    StockTradingPositionComponent,
    StockTradingNewPositionComponent,
    StockTradingPastComponent,
    StockTradingPendingComponent,
    StockTradingPerformanceComponent,
    StockTradingReviewComponent,
    StockTradingChartComponent,

    SymbolSearchComponent,
    TransactionsComponent,
    TermsComponent,
    
    FailuresuccesschainComponent,
    RecentSellsComponent,

    CryptoDashboardComponent,
    CryptoOwnershipGridComponent,
    CryptoDetailsComponent,
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    NgChartsModule,
    RouterModule.forRoot(routes, { relativeLinkResolution: 'legacy' })
  ],
  providers: [
    { provide: "windowObject", useValue: window},
    Title
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
