
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
import { PlaygroundComponent } from './playground/playground.component';
import { AdminEmailComponent } from './admin/email/admin-email.component';
import { AdminWeeklyComponent } from './admin/weekly/admin-weekly.component';
import { PaymentsComponent } from './payments/payments.component';
import { StockFundamentalsComponent } from './stocks/stock-details/stock-fundamentals.component';
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
import { StockTradingDashboardComponent } from './stocks/stock-trading/stock-trading-dashboard.component';
import { StockTradingPositionsComponent } from './stocks/stock-trading/stock-trading-positions.component';
import { StockTradingNewPositionComponent } from './stocks/stock-trading/stock-trading-new-position.component';
import { StockTradingPerformanceComponent } from './stocks/stock-trading-review/stock-trading-performance.component';
import { StockTradingReviewComponent } from './stocks/stock-trading-review/stock-trading-review.component';
import { StockTradingClosedPositionsComponent } from './stocks/stock-trading-review/stock-trading-closed-positions.component';
import { BrokerageOrdersComponent } from './brokerage/brokerage-orders.component';
import { StockViolationsComponent } from './stocks/stock-trading/stock-violations.component';
import { StockTransactionComponent } from './stocks/stock-details/stock-transaction.component';
import { StockTradingSimulatorComponent } from './stocks/stock-trading/stock-trading-simulator.component';
import { BrokerageNewOrderComponent } from './brokerage/brokerage-new-order.component';
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
import { DailyOutcomeScoresComponent } from './shared/reports/daily-outcome-scores.component';
import { StockTradingReviewDashboardComponent } from './stocks/stock-trading-review/stock-trading-review-dashboard.component';
import { StockTradingPendingPositionsComponent } from './stocks/stock-trading/stock-trading-pendingpositions.component';
import { StockSECFilingsComponent } from './stocks/stock-details/stock-secfilings.component';
import { StockTradingAnalysisDashboardComponent } from './stocks/stock-trading-analysis/stock-trading-analysis-dashboard.component';
import { RoutineDashboardComponent } from './routines/routines-dashboard.component';
import { StockTradingStrategiesComponent } from './shared/stocks/stock-trading-strategies.component';
import { StockTradingSummaryComponent } from './stocks/stock-trading/stock-trading-summary.component';
import { TradingViewLinkComponent } from './shared/stocks/trading-view-link.component';
import { OptionBrokeragePositionsComponent } from './options/option-dashboard/option-brokerage-positions.component';
import { StockLinkComponent } from './shared/stocks/stock-link.component';
import { RoutineComponent } from './routines/routines-routine.component';
import { OptionSpreadsComponent } from './options/option-chain/option-spreads.component';
import { OptionBrokerageOrdersComponent } from './options/option-dashboard/option-brokerage-orders.component';
import { RoutinesActiveRoutineComponent } from './routines/routines-active-routine.component';
import { LineChartComponent } from './shared/line-chart/line-chart.component';
import {CanvasJSAngularChartsModule} from "@canvasjs/angular-charts";
import {CandlestickChartComponent} from "./shared/candlestick-chart/candlestick-chart.component";
import {StockLinkAndTradingviewLinkComponent} from "./shared/stocks/stock-link-and-tradingview-link.component";
import {NgOptimizedImage} from "@angular/common";
import {StockSearchComponent} from "./stocks/stock-search/stock-search.component";
import {PageNotFoundComponent} from "./page-not-found/page-not-found.component";
import {InflectionPointsComponent} from "./playground/inflectionpoints.component";
import {LoadingComponent} from "./shared/loading/loading.component";
import {BrokerageAccountComponent} from "./brokerage/brokerage-account.component";


let routes: Routes = [
  {path: '', redirectTo: '/dashboard', pathMatch: 'full'},
  {path: 'admin', component: AdminDashboardComponent, canActivate: [AuthGuardAdminOnly]},
  {path: 'admin/email', component: AdminEmailComponent, canActivate: [AuthGuardAdminOnly]},
  {path: 'admin/users', component: AdminUsersComponent, canActivate: [AuthGuardAdminOnly]},
  {path: 'admin/weekly', component: AdminWeeklyComponent, canActivate: [AuthGuardAdminOnly]},
  {path: 'contact', component: ContactComponent},
  {path: 'dashboard', component: DashboardComponent, canActivate: [AuthGuard]},
  {path: 'events', component: EventsComponent, canActivate: [AuthGuard]},
  {path: 'landing', component: LandingComponent},
  {path: 'profile', component: ProfileComponent, canActivate: [AuthGuardUnverifiedAllowed]},
  {path: 'profile/create', component: ProfileCreateComponent},
  {path: 'profile/create/:plan', component: ProfileCreateComponent},
  {path: 'profile/login', component: ProfileLoginComponent},
  {path: 'profile/verify', component: ProfileVerifyComponent},
  {path: 'profile/passwordreset/:id', component: ProfilePasswordResetComponent},

  {path: 'options', component: OptionsComponent, canActivate: [AuthGuard]},
  {path: 'options/sell', component: OptionSellComponent, canActivate: [AuthGuard]},
  {path: 'options/sell/:ticker', component: OptionSellComponent, canActivate: [AuthGuard]},
  {path: 'options/chain/:ticker', component: OptionChainComponent, canActivate: [AuthGuard]},
  {path: 'optiondetails/:id', component: OwnedOptionComponent, canActivate: [AuthGuard]},

  {path: 'alerts', component: AlertsComponent, canActivate: [AuthGuardAdminOnly]},

  {path: 'summary', component: SummaryComponent, canActivate: [AuthGuard]},
  {path: 'payments', component: PaymentsComponent, canActivate: [AuthGuard]},

  {path: 'playground', component: PlaygroundComponent, canActivate: [AuthGuard]},
  {path: 'playground/inflectionpoints', component: InflectionPointsComponent, canActivate: [AuthGuard]},

  {path: 'privacy', component: PrivacyComponent},
  {path: 'trading', component: StockTradingDashboardComponent, canActivate: [AuthGuard]},
  {path: 'trading/analysis', component: StockTradingAnalysisDashboardComponent, canActivate: [AuthGuard]},
  {path: 'trading/simulations', component: StockTradingSimulationsComponent, canActivate: [AuthGuard]},
  {path: 'trading/simulator', component: StockTradingSimulatorComponent, canActivate: [AuthGuard]},
  {path: 'trading/review', component: StockTradingReviewDashboardComponent, canActivate: [AuthGuard]},
  {path: 'trading/review/:tab', component: StockTradingReviewDashboardComponent, canActivate: [AuthGuard]},
  {path: 'trading/:tab', component: StockTradingDashboardComponent, canActivate: [AuthGuard]},

  {path: 'stocks/lists', component: StockListsDashboardComponent, canActivate: [AuthGuard], title: 'Stock Lists'},
  {path: 'stocks/lists/:name', component: StockListComponent, canActivate: [AuthGuard], title: 'Stock Lists'},
  {path: 'stocks/newposition', component: StockNewPositionComponent, canActivate: [AuthGuard]},
  {path: 'stocks/:ticker', component: StockDetailsComponent, canActivate: [AuthGuard]},
  {path: 'stocks/:ticker/:tab', component: StockDetailsComponent, canActivate: [AuthGuard]},

  {path: 'transactions', component: TransactionsComponent, canActivate: [AuthGuard]},
  {path: 'terms', component: TermsComponent},
  {path: 'test', component: StockSearchComponent, canActivate: [AuthGuard]},

  {path: 'reports/chain', component: FailuresuccesschainComponent, canActivate: [AuthGuard], title: 'Chain Report'},
  {path: 'reports/recentsells', component: RecentSellsComponent, canActivate: [AuthGuard], title: 'Recent Sells'},
  {path: 'reports/outcomes', component: OutcomesReportComponent, canActivate: [AuthGuard], title: 'Outcomes Report'},
  {path: 'reports/gaps', component: GapsReportComponent, canActivate: [AuthGuard], title : 'Gaps Report'},

  {path: 'routines', component: RoutineDashboardComponent, canActivate: [AuthGuard], title: 'Routines'},
  {path: 'routines/:name/:mode', component: RoutineComponent, canActivate: [AuthGuard], title: 'Routines'},
  {path: 'routines/:name', component: RoutineComponent, canActivate: [AuthGuard], title: 'Routines'},

  {path: 'cryptos', component: CryptoDashboardComponent, canActivate: [AuthGuard]},
  {path: 'cryptos/:token', component: CryptoDetailsComponent, canActivate: [AuthGuard]},

  { path: '**', pathMatch: 'full', component: PageNotFoundComponent },
];

@NgModule({
  declarations: [
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

    OptionsComponent,
    OptionStatsComponent,
    OptionChainComponent,
    OptionSpreadsComponent,
    OptionSellComponent,
    OptionOpenComponent,
    OptionPerformanceComponent,
    OptionClosedComponent,

    PaymentsComponent,
    PrivacyComponent,
    ProfileComponent,
    ProfileCreateComponent,
    ProfileLoginComponent,
    ProfilePasswordResetComponent,
    ProfileVerifyComponent,
    SummaryComponent,
    OwnedOptionComponent,

    PlaygroundComponent,
    InflectionPointsComponent,

    StockDetailsComponent,
    StockFundamentalsComponent,
    StockPositionReportsComponent,
    StockOptionComponent,
    StockTransactionComponent,
    StockOwnershipComponent,
    StockViolationsComponent,

    StockTradingDashboardComponent,
    StockTradingPositionComponent,
    StockTradingPositionsComponent,
    StockTradingNewPositionComponent,
    StockTradingClosedPositionsComponent,
    StockTradingPerformanceComponent,
    StockTradingReviewComponent,
    StockTradingSummaryComponent,
    StockTradingSimulatorComponent,
    StockTradingSimulationsComponent,
    TradingPerformanceSummaryComponent,
    TradingActualVsSimulatedPositionComponent,

    BrokerageOrdersComponent,
    BrokerageNewOrderComponent,
    BrokerageAccountComponent,

    StockSearchComponent,
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
    CandlestickChartComponent,
    StockTradingReviewDashboardComponent,
    StockTradingPendingPositionsComponent,
    StockSECFilingsComponent,
    StockTradingAnalysisDashboardComponent,
    StockTradingStrategiesComponent,
    TradingViewLinkComponent,
    StockLinkComponent,
    StockLinkAndTradingviewLinkComponent,

    OptionBrokeragePositionsComponent,
    OptionBrokerageOrdersComponent,

    RoutineDashboardComponent,
    RoutineComponent,
    RoutinesActiveRoutineComponent,
    LineChartComponent,
    LoadingComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    FormsModule,
    CanvasJSAngularChartsModule,
    RouterModule.forRoot(routes, {}),
    NgOptimizedImage
  ],
  providers: [
    { provide: "windowObject", useValue: window},
    Title
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
