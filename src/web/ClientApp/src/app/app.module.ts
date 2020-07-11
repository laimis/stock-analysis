
import { AddNoteComponent } from './notes/add-note.component';
import { AppComponent } from './app.component';
import { AuthGuard, AuthGuardUnverifiedAllowed, AuthGuardAdminOnly } from './auth/auth.guard';
import { BrowserModule } from '@angular/platform-browser';
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
import { OptionDetailComponent } from './option-detail/option-detail.component';
import { OptionSellComponent } from './option-sell/option-sell.component';
import { ProfileComponent } from './profile/profile.component';
import { ProfileCreateComponent } from './profile/profile-create.component';
import { ProfileLoginComponent } from './profile/profile-login.component';
import { ReviewComponent } from './review/review.component';
import { RouterModule } from '@angular/router';
import { SoldOptionDetailComponent } from './sold-option-detail/sold-option-detail.component';
import { StockDetailsComponent } from './stock-details/stock-details.component';
import { StockListComponent } from './stock-lists/stock-list.component';
import { StockListsComponent } from './stock-lists/stock-lists.component';
import { TransactionsComponent } from './transactions/transactions.component';
import { ProfilePasswordResetComponent } from './profile/profile-passwordreset.component';
import { ProfileVerifyComponent } from './profile/profile-verify.component';
import { ContactComponent } from './contact/contact.component';
import { TermsComponent } from './terms/terms.component';
import { PrivacyComponent } from './privacy/privacy.component';
import { SymbolSearchComponent } from './symbol-search/symbol-search.component';
import { PlaygroundComponent } from './playground/playground.component';
import { OwnedStockDetailComponent } from './owned-stock-detail/owned-stock-detail.component';
import { AdminEmailComponent } from './admin/email/admin-email.component';
import { AdminWeeklyComponent } from './admin/weekly/admin-weekly.component';
import { PaymentsComponent } from './payments/payments.component';
import { StockFundamentalsComponent } from './stock-details/stock-fundamentals.component';
import { StockNotesComponent } from './stock-details/stock-notes.component';
import { StockOwnershipComponent } from './stock-details/stock-ownership.component';
import { StockOptionComponent } from './stock-details/stock-option.component';
import { StockGridComponent } from './stock-grid/stock-grid.component';
import { StockAlertsComponent } from './stock-details/stock-alerts.component';
import { AlertsComponent } from './alerts/alerts.component';
import { AdminUsersComponent } from './admin/users/admin-users.component';
import { OptionsComponent } from './options/options.component';
import { OptionStatsComponent } from './options/option-stats.component';

var routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full', canActivate: [AuthGuard] },
  { path: 'alerts', component: AlertsComponent, canActivate: [AuthGuard] },
  { path: 'admin/email', component: AdminEmailComponent, canActivate: [AuthGuardAdminOnly] },
  { path: 'admin/users', component: AdminUsersComponent, canActivate: [AuthGuardAdminOnly] },
  { path: 'admin/weekly', component: AdminWeeklyComponent, canActivate: [AuthGuardAdminOnly] },
  { path: 'contact', component: ContactComponent },
  { path: 'dashboard', component: DashboardComponent, canActivate: [AuthGuard] },
  { path: 'events', component: EventsComponent, canActivate: [AuthGuard]},
  { path: 'grid', component: StockGridComponent, canActivate: [AuthGuard]},
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
  { path: 'options/:ticker', component: OptionDetailComponent, canActivate: [AuthGuard] },
  { path: 'optiondetails/:id', component: SoldOptionDetailComponent, canActivate: [AuthGuard]},

  { path: 'review', component: ReviewComponent, canActivate: [AuthGuard] },
  { path: 'payments', component: PaymentsComponent, canActivate: [AuthGuard]},
  { path: 'playground', component: PlaygroundComponent},
  { path: 'privacy', component: PrivacyComponent},
  { path: 'stockdetails/:id', component: OwnedStockDetailComponent},

  { path: 'stocks/lists', component: StockListsComponent, canActivate: [AuthGuard] },
  { path: 'stocks/lists', component: StockListsComponent, canActivate: [AuthGuard] },
  { path: 'stocks/:ticker', component: StockDetailsComponent, canActivate: [AuthGuard] },

  { path: 'transactions', component: TransactionsComponent, canActivate: [AuthGuard] },
  { path: 'terms', component: TermsComponent},
  { path: 'test', component: SymbolSearchComponent}
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
    OptionDetailComponent,
    OptionSellComponent,
    OwnedStockDetailComponent,
    PaymentsComponent,
    PlaygroundComponent,
    PrivacyComponent,
    ProfileComponent,
    ProfileCreateComponent,
    ProfileLoginComponent,
    ProfilePasswordResetComponent,
    ProfileVerifyComponent,
    ReviewComponent,
    SoldOptionDetailComponent,
    StockAlertsComponent,
    StockDetailsComponent,
    StockFundamentalsComponent,
    StockGridComponent,
    StockListComponent,
    StockListsComponent,
    StockNotesComponent,
    StockOptionComponent,
    StockOwnershipComponent,
    SymbolSearchComponent,
    TransactionsComponent,
    TermsComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    RouterModule.forRoot(routes)
  ],
  providers: [
	  { provide: "windowObject", useValue: window}
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
