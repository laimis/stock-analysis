import { Component, OnInit, inject } from '@angular/core';
import {Title} from '@angular/platform-browser';
import {Router, RouterLink} from '@angular/router';
import {PortfolioHoldings, Reminder, StocksService} from '../services/stocks.service';
import { AlertsComponent } from "../alerts/alerts.component";
import { TimeAgoPipe } from '../services/time-ago.pipe';
import { StockLinkAndTradingviewLinkComponent } from '../shared/stocks/stock-link-and-tradingview-link.component';


@Component({
    selector: 'app-dashboard',
    templateUrl: './dashboard.component.html',
    styleUrls: ['./dashboard.component.css'],
    imports: [RouterLink, AlertsComponent, TimeAgoPipe, StockLinkAndTradingviewLinkComponent],
})
export class DashboardComponent implements OnInit {
    private stocks = inject(StocksService);
    private router = inject(Router);
    private title = inject(Title);


    dashboard: PortfolioHoldings
    loaded: boolean = false
    upcomingReminders: Reminder[] = []

    toolLinks = [
        {path: '/summary', label: 'Weekly Summary'},
        {path: '/trading/review', label: 'Trading Review'},
        {path: '/stocks/analysis', label: 'Stock Position Analysis'},
        {path: '/stocks/tradingsimulations', label: 'Simulations'},
        {path: '/stocks/simulator', label: 'SimulatOR'},
        {path: '/stocks/lists', label: 'Stock Lists'},
        {path: '/reports/recentsells', label: 'Recent Sells'},
        {path: '/transactions', label: 'Transactions'},
        {path: '/reports/chain', label: 'Chain'},
        {path: '/reports/trends', label: 'Trends'},
    ];

    ngOnInit() {

        this.title.setTitle("Dashboard - Nightingale Trading")

        this.stocks.getPortfolioHoldings().subscribe(result => {
            this.dashboard = result;
            this.loaded = true;
        }, error => {
            console.log(error);
            this.loaded = false;
        })

        this.stocks.getReminders().subscribe(reminders => {
            const today = new Date();
            const pad = (n: number) => String(n).padStart(2, '0');
            const toDateStr = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
            const todayStr = toDateStr(today);
            const yesterday = new Date(today);
            yesterday.setDate(today.getDate() - 1);
            const yesterdayStr = toDateStr(yesterday);
            const in7Days = new Date(today);
            in7Days.setDate(today.getDate() + 7);
            const in7DaysStr = toDateStr(in7Days);
            this.upcomingReminders = reminders.filter(r => {
                const dateStr = r.date.substring(0, 10);
                return dateStr >= yesterdayStr && dateStr <= in7DaysStr;
            }).sort((a, b) => a.date.localeCompare(b.date));
        }, error => {
            console.log(error);
        });
    }

    onTickerSelected(ticker: string) {
        this.router.navigateByUrl('/stocks/' + ticker)
    }

    dismissReminder(reminder: Reminder) {
        this.stocks.deleteReminder(reminder.reminderId).subscribe({
            next: () => {
                this.upcomingReminders = this.upcomingReminders.filter(r => r.reminderId !== reminder.reminderId);
            },
            error: (err) => {
                console.log(err);
            }
        });
    }
}
