import {Routes} from "@angular/router";
import {AdminDashboardComponent} from "./admin/admin-dashboard.component";
import {AuthGuard, AuthGuardAdminOnly, AuthGuardUnverifiedAllowed, WithLoginStatus} from "./auth/auth.guard";
import {AdminEmailComponent} from "./admin/email/admin-email.component";
import {AdminUsersComponent} from "./admin/users/admin-users.component";
import {AdminWeeklyComponent} from "./admin/weekly/admin-weekly.component";
import {ContactComponent} from "./contact/contact.component";
import {DashboardComponent} from "./dashboard/dashboard.component";
import {EventsComponent} from "./events/events.component";
import {LandingComponent} from "./landing/landing.component";
import {ProfileComponent} from "./profile/profile.component";
import {ProfileCreateComponent} from "./profile/profile-create.component";
import {ProfileLoginComponent} from "./profile/profile-login.component";
import {ProfileVerifyComponent} from "./profile/profile-verify.component";
import {ProfilePasswordResetComponent} from "./profile/profile-passwordreset.component";
import {AccountTransactionsComponent} from "./profile/account-transactions.component";
import {OptionsComponent} from "./options/option-dashboard/option-dashboard.component";
import {OptionSellComponent} from "./options/option-sell/option-sell.component";
import {OptionChainComponent} from "./options/option-chain/option-chain.component";
import {OwnedOptionComponent} from "./options/owned-option-detail/owned-option-detail.component";
import {AlertsComponent} from "./alerts/alerts.component";
import {StockPriceAlertsComponent} from "./alerts/stock-price-alerts.component";
import {SummaryComponent} from "./summary/summary.component";
import {PlaygroundComponent} from "./playground/playground.component";
import {InflectionPointsComponent} from "./playground/inflectionpoints.component";
import {PrivacyComponent} from "./privacy/privacy.component";
import {
    StockTradingReviewDashboardComponent
} from "./stocks/stock-trading-review/stock-trading-review-dashboard.component";
import {StockTradingSimulationsComponent} from "./stocks/stock-trading/stock-trading-simulations.component";
import {StockTradingSimulatorComponent} from "./stocks/stock-trading/stock-trading-simulator.component";
import {
    StockTradingAnalysisDashboardComponent
} from "./stocks/stock-trading-analysis/stock-trading-analysis-dashboard.component";
import {StockTradingDashboardComponent} from "./stocks/stock-trading/stock-trading-dashboard.component";
import {StockListsDashboardComponent} from "./stocks/stock-lists/stock-lists-dashboard/stock-lists-dashboard.component";
import {StockListComponent} from "./stocks/stock-lists/stock-list/stock-list.component";
import {
    StockTradingPendingPositionsDashboardComponent
} from "./stocks/stock-trading-dashboard/stock-trading-pending-positions-dashboard.component";
import {StockDetailsComponent} from "./stocks/stock-details/stock-details.component";
import {TransactionsComponent} from "./transactions/transactions.component";
import {TermsComponent} from "./terms/terms.component";
import {StockSearchComponent} from "./stocks/stock-search/stock-search.component";
import {
    FailuresuccesschainComponent
} from "./reports/failuresuccesschain/failuresuccesschain/failuresuccesschain.component";
import {RecentSellsComponent} from "./recentsells/recentsells.component";
import {OutcomesReportComponent} from "./reports/outcomes-report/outcomes-report.component";
import {GapsReportComponent} from "./reports/gaps/gaps-report.component";
import {TradesReportComponent} from "./reports/trades-report/trades-report.component";
import {TrendsReportComponent} from "./reports/trends-report/trends-report.component";
import {RoutineDashboardComponent} from "./routines/routines-dashboard.component";
import {RoutineComponent} from "./routines/routines-routine.component";
import {PageNotFoundComponent} from "./page-not-found/page-not-found.component";
import {OptionSpreadBuilderComponent} from "./options/option-spread-builder/option-spread-builder.component";

export const routes: Routes = [
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
    {path: 'profile/transactions', component: AccountTransactionsComponent, canActivate: [AuthGuard]},

    {path: 'options', component: OptionsComponent, canActivate: [AuthGuard]},
    {path: 'options/sell', component: OptionSellComponent, canActivate: [AuthGuard]},
    {path: 'options/sell/:ticker', component: OptionSellComponent, canActivate: [AuthGuard]},
    {path: 'options/chainold/:ticker', component: OptionChainComponent, canActivate: [AuthGuard]},
    {path: 'options/spreadbuilder/:ticker', component: OptionSpreadBuilderComponent, canActivate: [AuthGuard]},
    {path: 'options/:tab', component: OptionsComponent, canActivate: [AuthGuard]},
    {path: 'options/:tab/:ticker', component: OptionsComponent, canActivate: [AuthGuard]},
    {path: 'optiondetails/:id', component: OwnedOptionComponent, canActivate: [AuthGuard]},

    {path: 'alerts', component: AlertsComponent, canActivate: [AuthGuardAdminOnly]},
    {path: 'alerts/price', component: StockPriceAlertsComponent, canActivate: [AuthGuard]},

    {path: 'summary', component: SummaryComponent, canActivate: [AuthGuard]},

    {path: 'playground', component: PlaygroundComponent, canActivate: [AuthGuard]},
    {path: 'playground/inflectionpoints', component: InflectionPointsComponent, canActivate: [AuthGuard]},

    {path: 'privacy', component: PrivacyComponent},
    {
        path: 'trading/review',
        component: StockTradingReviewDashboardComponent,
        canActivate: [AuthGuard],
        title: 'Review'
    },
    {
        path: 'trading/review/:tab',
        component: StockTradingReviewDashboardComponent,
        canActivate: [AuthGuard],
        title: 'Review'
    },
    {path: 'stocks/tradingsimulations', component: StockTradingSimulationsComponent, canActivate: [AuthGuard], title: 'Trading Simulations'},
    {path: 'stocks/simulator', component: StockTradingSimulatorComponent, canActivate: [AuthGuard], title : 'Simulator'},
    {path: 'stocks/analysis', component: StockTradingAnalysisDashboardComponent, canActivate: [AuthGuard], title: 'Stock Position Analysis'},
    {path: 'stocks/positions', component: StockTradingDashboardComponent, canActivate: [AuthGuard], title: 'Stock Positions'},
    {path: 'stocks/positions/:tab', component: StockTradingDashboardComponent, canActivate: [AuthGuard], title: 'Stock Positions'},
    {path: 'stocks/lists', component: StockListsDashboardComponent, canActivate: [AuthGuard], title: 'Stock Lists'},
    {path: 'stocks/lists/:id', component: StockListComponent, canActivate: [AuthGuard], title: 'Stock Lists'},
    {path: 'stocks/newposition', component: StockTradingPendingPositionsDashboardComponent, canActivate: [AuthGuard], title: 'Stock Trading'},
    {path: 'stocks/newposition/:tab', component: StockTradingPendingPositionsDashboardComponent, canActivate: [AuthGuard], title: 'Stock Trading'},
    {path: 'stocks/:ticker', component: StockDetailsComponent, canActivate: [AuthGuard]},
    {path: 'stocks/:ticker/:tab', component: StockDetailsComponent, canActivate: [AuthGuard]},

    {path: 'transactions', component: TransactionsComponent, canActivate: [AuthGuard]},
    {path: 'terms', component: TermsComponent},
    {path: 'test', component: StockSearchComponent, canActivate: [AuthGuard]},

    {path: 'reports/chain', component: FailuresuccesschainComponent, canActivate: [AuthGuard], title: 'Chain Report'},
    {path: 'reports/recentsells', component: RecentSellsComponent, canActivate: [AuthGuard], title: 'Recent Sells'},
    {path: 'reports/outcomes', component: OutcomesReportComponent, canActivate: [AuthGuard], title: 'Outcomes Report'},
    {path: 'reports/gaps', component: GapsReportComponent, canActivate: [AuthGuard], title: 'Gaps Report'},
    {path: 'reports/trades', component: TradesReportComponent, canActivate: [AuthGuard], title: 'Trades Report'},
    {path: 'reports/trends', component: TrendsReportComponent, canActivate: [AuthGuard], title: 'Trends Report'},

    {path: 'routines', component: RoutineDashboardComponent, canActivate: [AuthGuard], title: 'Routines'},
    {path: 'routines/:id/:mode', component: RoutineComponent, canActivate: [AuthGuard], title: 'Routines'},
    {path: 'routines/:id', component: RoutineComponent, canActivate: [AuthGuard], title: 'Routines'},

    {path: '**', pathMatch: 'full', component: PageNotFoundComponent, canActivate: [WithLoginStatus]},
];
