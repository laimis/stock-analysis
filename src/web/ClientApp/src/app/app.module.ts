
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
import { StockPositionReportsComponent } from './stocks/stock-trading/stock-trading-outcomes-reports.component';
import { AdminUsersComponent } from './admin/users/admin-users.component';
import { OptionsComponent } from './options/option-dashboard/option-dashboard.component';
import { OptionStatsComponent } from './options/option-dashboard/option-stats.component';
import { OptionOpenComponent } from './options/option-dashboard/option-open.component';
import { OptionPerformanceComponent } from './options/option-dashboard/option-performance.component';
import { OptionClosedComponent } from './options/option-dashboard/option-closed.component';
import { FailuresuccesschainComponent } from './reports/failuresuccesschain/failuresuccesschain/failuresuccesschain.component';
import { RecentSellsComponent } from './recentsells/recentsells.component';
import { CryptoDashboardComponent } from './cryptos/crypto-dashboard/crypto-dashboard.component';
import { CryptoOwnershipGridComponent } from './cryptos/crypto-dashboard/crypto-ownership-grid.component';
import { CryptoDetailsComponent } from './cryptos/crypto-details/crypto-details.component';
import { NgChartsModule } from 'ng2-charts';
import { StockTradingComponent } from './stocks/stock-trading/stock-trading-dashboard.component';
import { StockTradingPositionsComponent } from './stocks/stock-trading/stock-trading-positions.component';
import { StockTradingNewPositionComponent } from './stocks/stock-trading/stock-trading-newposition.component';
import { StockTradingPerformanceComponent } from './stocks/stock-trading-review/stock-trading-performance.component';
import { StockTradingReviewComponent } from './stocks/stock-trading-review/stock-trading-review.component';
import { StockChartComponent } from './shared/stocks/stock-chart.component';
import { StockTradingOpenPositionsComponent } from './stocks/stock-trading/stock-trading-open-positions.component';
import { StockTradingClosedPositionsComponent } from './stocks/stock-trading-review/stock-trading-closed-positions.component';
import { BrokerageOrdersComponent } from './brokerage/orders.component';
import { StockViolationsComponent } from './stocks/stock-trading/stock-violations.component';
import { StockTransactionComponent } from './stocks/stock-details/stock-transaction.component';
import { StockTradingSimulatorComponent } from './stocks/stock-trading/stock-trading-simulator.component';
import { BrokerageNewOrderComponent } from './brokerage/neworder.component';
import { StockAnalysisComponent } from './stocks/stock-details/stock-analysis.component';
import { StockTradingPositionComponent } from './stocks/stock-trading/stock-trading-position.component';
import { AlertsComponent } from './alerts/alerts.component';
import { OutcomesComponent } from './shared/reports/outcomes.component';
import { OutcomesAnalysisReportComponent } from './shared/reports/outcomes-analysis-report.component';
import { AdminDashboardComponent } from './admin/admin-dashboard.component';
import { OutcomesReportComponent } from './reports/outcomes-report/outcomes-report.component';
import { GapsReportComponent } from './reports/gaps/gaps-report.component';
import { GapsComponent } from './shared/reports/gaps.component';
import { PercentChangeDistributionComponent } from './shared/reports/percent-change-distribution.component';
import { StockNewPositionComponent } from './stocks/stock-buy/stock-new-position.component';
import { TradingPerformanceSummaryComponent } from './shared/stocks/trading-performance-summary.component';
import { TradingActualVsSimulatedPositionComponent } from './shared/stocks/trading-actual-vs-simulated.component';
import { StockListsDashboardComponent } from './stocks/stock-lists/stock-lists-dashboard/stock-lists-dashboard.component';
import { StockListComponent } from './stocks/stock-lists/stock-list/stock-list.component';
import { StockTradingSimulationsComponent } from './stocks/stock-trading/stock-trading-simulations.component';
import { ChartComponent } from './shared/chart/chart.component';
import { DailyOutcomeScoresComponent } from './shared/reports/daily-outcome-scores.component';
import { StockTradingReviewDashboardComponent } from './stocks/stock-trading-review/stock-trading-review-dashboard.component';
import { StockTradingPendingPositionsComponent } from './stocks/stock-trading/stock-trading-pendingpositions.component';


var routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'admin', component: AdminDashboardComponent, canActivate: [AuthGuardAdminOnly] },
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

  { path: 'alerts', component: AlertsComponent, canActivate: [AuthGuardAdminOnly] },

  { path: 'summary', component: SummaryComponent, canActivate: [AuthGuard] },
  { path: 'payments', component: PaymentsComponent, canActivate: [AuthGuard]},
  { path: 'playground', component: PlaygroundComponent, canActivate: [AuthGuard]},
  { path: 'privacy', component: PrivacyComponent},
  { path: 'trading', component: StockTradingComponent, canActivate: [AuthGuard]},
  { path: 'trading/simulations', component: StockTradingSimulationsComponent, canActivate: [AuthGuard]},
  { path: 'trading/simulator', component: StockTradingSimulatorComponent, canActivate: [AuthGuard]},
  { path: 'trading/review', component: StockTradingReviewDashboardComponent, canActivate: [AuthGuard]},
  { path: 'trading/:tab', component: StockTradingComponent, canActivate: [AuthGuard]},

  { path: 'stocks/lists', component: StockListsDashboardComponent, canActivate: [AuthGuard]},
  { path: 'stocks/lists/:name', component: StockListComponent, canActivate: [AuthGuard]},
  { path: 'stocks/newposition', component: StockNewPositionComponent, canActivate: [AuthGuard]},
  { path: 'stocks/:ticker', component: StockDetailsComponent, canActivate: [AuthGuard] },
  { path: 'stocks/:ticker/:tab', component: StockDetailsComponent, canActivate: [AuthGuard] },

  { path: 'transactions', component: TransactionsComponent, canActivate: [AuthGuard] },
  { path: 'terms', component: TermsComponent},
  { path: 'test', component: SymbolSearchComponent},

  { path: 'reports/chain', component: FailuresuccesschainComponent, canActivate: [AuthGuard]},
  { path: 'reports/recentsells', component: RecentSellsComponent, canActivate: [AuthGuard]},
  { path: 'reports/outcomes', component: OutcomesReportComponent, canActivate: [AuthGuard]},
  { path: 'reports/gaps', component: GapsReportComponent, canActivate: [AuthGuard]},

  { path: 'cryptos', component: CryptoDashboardComponent, canActivate: [AuthGuard]},
  { path: 'cryptos/:token', component: CryptoDetailsComponent, canActivate: [AuthGuard]},
]

@NgModule({
  declarations: [
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

    StockDetailsComponent,
    StockFundamentalsComponent,
    StockPositionReportsComponent,
    StockNotesComponent,
    StockOptionComponent,
    StockTransactionComponent,
    StockOwnershipComponent,
    StockTradingOpenPositionsComponent,
    StockViolationsComponent,
    
    StockTradingComponent,
    StockTradingPositionComponent,
    StockTradingPositionsComponent,
    StockTradingNewPositionComponent,
    StockTradingClosedPositionsComponent,
    StockTradingPerformanceComponent,
    StockTradingReviewComponent,
    StockChartComponent,
    StockTradingSimulatorComponent,
    StockTradingSimulationsComponent,
    TradingPerformanceSummaryComponent,
    TradingActualVsSimulatedPositionComponent,
    
    BrokerageOrdersComponent,
    BrokerageNewOrderComponent,

    SymbolSearchComponent,
    TransactionsComponent,
    TermsComponent,
    
    FailuresuccesschainComponent,
    RecentSellsComponent,

    CryptoDashboardComponent,
    CryptoOwnershipGridComponent,
    CryptoDetailsComponent,
    
    StockAnalysisComponent,
    OutcomesComponent,
    OutcomesReportComponent,
    OutcomesAnalysisReportComponent,
    GapsReportComponent,
    GapsComponent,
    PercentChangeDistributionComponent,
    DailyOutcomeScoresComponent,

    AlertsComponent,
    StockNewPositionComponent,
    StockListsDashboardComponent,
    StockListComponent,
    ChartComponent,
    StockTradingReviewDashboardComponent,
    StockTradingPendingPositionsComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    NgChartsModule,
    RouterModule.forRoot(routes, { })
  ],
  providers: [
    { provide: "windowObject", useValue: window},
    Title
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
