import {AppComponent} from './app.component';
import {BrowserModule, Title} from '@angular/platform-browser';
import {DashboardComponent} from './dashboard/dashboard.component';
import {ErrorDisplayComponent} from './shared/error-display/error-display.component';
import {EventsComponent} from './events/events.component';
import {FormsModule, ReactiveFormsModule} from '@angular/forms';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import {LandingComponent} from './landing/landing.component';
import {NavMenuComponent} from './nav-menu/nav-menu.component';
import {NgModule} from '@angular/core';
import {OptionChainComponent} from './options/option-chain/option-chain.component';
import {OptionSellComponent} from './options/option-sell/option-sell.component';
import {ProfileComponent} from './profile/profile.component';
import {ProfileCreateComponent} from './profile/profile-create.component';
import {ProfileLoginComponent} from './profile/profile-login.component';
import {SummaryComponent} from './summary/summary.component';
import {RouterModule} from '@angular/router';
import {OwnedOptionComponent} from './options/owned-option-detail/owned-option-detail.component';
import {StockDetailsComponent} from './stocks/stock-details/stock-details.component';
import {TransactionsComponent} from './transactions/transactions.component';
import {ProfilePasswordResetComponent} from './profile/profile-passwordreset.component';
import {ProfileVerifyComponent} from './profile/profile-verify.component';
import {ContactComponent} from './contact/contact.component';
import {TermsComponent} from './terms/terms.component';
import {PrivacyComponent} from './privacy/privacy.component';
import {PlaygroundComponent} from './playground/playground.component';
import {AdminEmailComponent} from './admin/email/admin-email.component';
import {AdminWeeklyComponent} from './admin/weekly/admin-weekly.component';
import {StockFundamentalsComponent} from './stocks/stock-details/stock-fundamentals.component';
import {StockOwnershipComponent} from './stocks/stock-details/stock-ownership.component';
import {StockOptionComponent} from './stocks/stock-details/stock-option.component';
import {StockPositionReportsComponent} from './stocks/stock-trading/stock-trading-outcomes-reports.component';
import {AdminUsersComponent} from './admin/users/admin-users.component';
import {OptionsComponent} from './options/option-dashboard/option-dashboard.component';
import {OptionStatsComponent} from './options/option-dashboard/option-stats.component';
import {OptionOpenComponent} from './options/option-dashboard/option-open.component';
import {OptionPerformanceComponent} from './options/option-dashboard/option-performance.component';
import {OptionClosedComponent} from './options/option-dashboard/option-closed.component';
import {
    FailuresuccesschainComponent
} from './reports/failuresuccesschain/failuresuccesschain/failuresuccesschain.component';
import {RecentSellsComponent} from './recentsells/recentsells.component';
import {CryptoDashboardComponent} from './cryptos/crypto-dashboard/crypto-dashboard.component';
import {CryptoOwnershipGridComponent} from './cryptos/crypto-dashboard/crypto-ownership-grid.component';
import {CryptoDetailsComponent} from './cryptos/crypto-details/crypto-details.component';
import {StockTradingPositionsComponent} from './stocks/stock-trading/stock-trading-positions.component';
import {StockTradingPerformanceComponent} from './stocks/stock-trading-review/stock-trading-performance.component';
import {StockTradingReviewComponent} from './stocks/stock-trading-review/stock-trading-review.component';
import {
    StockTradingClosedPositionsComponent
} from './stocks/stock-trading-review/stock-trading-closed-positions.component';
import {BrokerageOrdersComponent} from './brokerage/brokerage-orders.component';
import {StockViolationsComponent} from './stocks/stock-trading/stock-violations.component';
import {StockTransactionComponent} from './stocks/stock-details/stock-transaction.component';
import {StockTradingSimulatorComponent} from './stocks/stock-trading/stock-trading-simulator.component';
import {BrokerageNewOrderComponent} from './brokerage/brokerage-new-order.component';
import {StockAnalysisComponent} from './stocks/stock-details/stock-analysis.component';
import {StockTradingPositionComponent} from './stocks/stock-trading/stock-trading-position.component';
import {AlertsComponent} from './alerts/alerts.component';
import {OutcomesComponent} from './shared/reports/outcomes.component';
import {OutcomesAnalysisReportComponent} from './shared/reports/outcomes-analysis-report.component';
import {AdminDashboardComponent} from './admin/admin-dashboard.component';
import {OutcomesReportComponent} from './reports/outcomes-report/outcomes-report.component';
import {GapsReportComponent} from './reports/gaps/gaps-report.component';
import {GapsComponent} from './shared/reports/gaps.component';
import {PercentChangeDistributionComponent} from './shared/reports/percent-change-distribution.component';
import {TradingPerformanceSummaryComponent} from './shared/stocks/trading-performance-summary.component';
import {TradingActualVsSimulatedPositionComponent} from './shared/stocks/trading-actual-vs-simulated.component';
import {StockListsDashboardComponent} from './stocks/stock-lists/stock-lists-dashboard/stock-lists-dashboard.component';
import {StockListComponent} from './stocks/stock-lists/stock-list/stock-list.component';
import {StockTradingSimulationsComponent} from './stocks/stock-trading/stock-trading-simulations.component';
import {
    StockTradingReviewDashboardComponent
} from './stocks/stock-trading-review/stock-trading-review-dashboard.component';
import {StockSECFilingsComponent} from './stocks/stock-details/stock-secfilings.component';
import {
    StockTradingAnalysisDashboardComponent
} from './stocks/stock-trading-analysis/stock-trading-analysis-dashboard.component';
import {RoutineDashboardComponent} from './routines/routines-dashboard.component';
import {StockTradingStrategiesComponent} from './shared/stocks/stock-trading-strategies.component';
import {StockTradingSummaryComponent} from './stocks/stock-trading/stock-trading-summary.component';
import {TradingViewLinkComponent} from './shared/stocks/trading-view-link.component';
import {OptionBrokeragePositionsComponent} from './options/option-dashboard/option-brokerage-positions.component';
import {StockLinkComponent} from './shared/stocks/stock-link.component';
import {RoutineComponent} from './routines/routines-routine.component';
import {OptionSpreadsComponent} from './options/option-chain/option-spreads.component';
import {OptionBrokerageOrdersComponent} from './options/option-dashboard/option-brokerage-orders.component';
import {RoutinesActiveRoutineComponent} from './routines/routines-active-routine.component';
import {LineChartComponent} from './shared/line-chart/line-chart.component';
import {CanvasJSAngularChartsModule} from "@canvasjs/angular-charts";
import {CandlestickChartComponent} from "./shared/candlestick-chart/candlestick-chart.component";
import {StockLinkAndTradingviewLinkComponent} from "./shared/stocks/stock-link-and-tradingview-link.component";
import {NgOptimizedImage} from "@angular/common";
import {StockSearchComponent} from "./stocks/stock-search/stock-search.component";
import {InflectionPointsComponent} from "./playground/inflectionpoints.component";
import {TradesReportComponent} from "./reports/trades-report/trades-report.component";
import {LoadingComponent} from "./shared/loading/loading.component";
import {StockTradingChartsComponent} from "./stocks/stock-trading/stock-trading-charts.component";
import {CorrelationsComponent} from "./shared/reports/correlations.component";
import {StockTradingNewPositionComponent} from "./stocks/stock-trading/stock-trading-new-position.component";
import {StockTradingDashboardComponent} from "./stocks/stock-trading/stock-trading-dashboard.component";
import {ParsedDatePipe} from "./services/parsedDate.filter";
import {OptionSpreadBuilderComponent} from "./options/option-spread-builder/option-spread-builder.component";
import {routes} from "./app.routes";
import {DailyOutcomeScoresComponent} from "./shared/reports/daily-outcome-scores.component";
import {OptionPositionComponent} from "./options/option-position/option-position.component";


@NgModule({
    declarations: [
        AdminEmailComponent,
        AdminWeeklyComponent,
        AdminUsersComponent,
        AdminDashboardComponent,
        AppComponent,
        ContactComponent,
        DashboardComponent,
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
        StockTradingPositionComponent,
        StockTradingPositionsComponent,
        StockTradingClosedPositionsComponent,
        StockTradingDashboardComponent,
        StockTradingPerformanceComponent,
        StockTradingReviewComponent,
        StockTradingSummaryComponent,
        StockTradingSimulatorComponent,
        StockTradingSimulationsComponent,
        TradingPerformanceSummaryComponent,
        TradingActualVsSimulatedPositionComponent,
        TransactionsComponent,
        TermsComponent,
        FailuresuccesschainComponent,
        RecentSellsComponent,
        CryptoDashboardComponent,
        CryptoOwnershipGridComponent,
        CryptoDetailsComponent,
        OutcomesReportComponent,
        GapsReportComponent,
        TradesReportComponent,
        AlertsComponent,
        StockListsDashboardComponent,
        StockListComponent,
        StockTradingReviewDashboardComponent,
        StockSECFilingsComponent,
        StockTradingAnalysisDashboardComponent,
        StockTradingStrategiesComponent,
        OptionBrokeragePositionsComponent,
        OptionBrokerageOrdersComponent,
        RoutineDashboardComponent,
        RoutineComponent,
        RoutinesActiveRoutineComponent,
    ],
    bootstrap: [AppComponent],
    imports: [
        BrowserModule,
        FormsModule,
        StockSearchComponent,
        LoadingComponent,
        LineChartComponent,
        ErrorDisplayComponent,
        CanvasJSAngularChartsModule,
        RouterModule.forRoot(routes, {}),
        NgOptimizedImage,
        StockTradingChartsComponent,
        ReactiveFormsModule,
        StockLinkComponent,
        TradingViewLinkComponent,
        StockLinkAndTradingviewLinkComponent,
        CorrelationsComponent,
        StockTradingNewPositionComponent,
        BrokerageOrdersComponent,
        CandlestickChartComponent,
        GapsComponent,
        ParsedDatePipe,
        OutcomesAnalysisReportComponent,
        OutcomesComponent,
        BrokerageNewOrderComponent,
        OptionSpreadBuilderComponent,
        StockAnalysisComponent,
        PercentChangeDistributionComponent,
        DailyOutcomeScoresComponent,
        OptionPositionComponent
    ],
    providers: [
        {provide: "windowObject", useValue: window},
        Title,
        provideHttpClient(withInterceptorsFromDi())
    ]
})
export class AppModule {
}
