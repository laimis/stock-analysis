
import { AppComponent } from './app.component';
import { AuthGuard } from './auth/auth.guard';
import { BrowserModule } from '@angular/platform-browser';
import { DashboardComponent } from './dashboard/dashboard.component';
import { ErrorDisplayComponent } from './shared/error-display/error-display.component';
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
import { StockTransactionComponent } from './stock-transaction/stock-transaction.component';
import { StockListsComponent } from './stock-lists/stock-lists.component';
import { StockListComponent } from './stock-lists/stock-list.component';
import { AddNoteComponent } from './notes/add-note.component';
import { NotesComponent } from './notes/notes.component';
import { NoteComponent } from './notes/note.component';
import { TransactionsComponent } from './transactions/transactions.component';
import { EventsComponent } from './events/events.component';
import { ReviewComponent } from './review/review.component';

var routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full', canActivate: [AuthGuard] },
  { path: 'dashboard', component: DashboardComponent, canActivate: [AuthGuard] },
  { path: 'events', component: EventsComponent},
  { path: 'profile', component: ProfileComponent},
  { path: 'notes', component: NotesComponent},
  { path: 'notes/add', component: AddNoteComponent},
  { path: 'notes/add/:ticker', component: AddNoteComponent},
  { path: 'notes/filtered/:ticker', component: NotesComponent},
  { path: 'notes/:id', component: NoteComponent},
  { path: 'options/sell', component: OptionSellComponent },
  { path: 'options/:ticker', component: OptionDetailComponent },
  { path: 'review', component: ReviewComponent },
  { path: 'soldoptions/:ticker/:type/:strikePrice/:expiration', component: SoldOptionDetailComponent},
  { path: 'stocks/lists', component: StockListsComponent },
  { path: 'stocks/transaction', component: StockTransactionComponent },
  { path: 'stocks/transaction/:ticker', component: StockTransactionComponent },
  { path: 'stocks/:ticker', component: StockDetailsComponent },
  { path: 'transactions', component: TransactionsComponent },
]

@NgModule({
  declarations: [
    AddNoteComponent,
    AppComponent,
    DashboardComponent,
    ErrorDisplayComponent,
    EventsComponent,
    NavMenuComponent,
    NoteComponent,
    NotesComponent,
    OptionDetailComponent,
    OptionSellComponent,
    ProfileComponent,
    ReviewComponent,
    SoldOptionDetailComponent,
    StockDetailsComponent,
    StockListComponent,
    StockListsComponent,
    StockTransactionComponent,
    TransactionsComponent
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
