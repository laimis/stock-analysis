import {ApplicationConfig, enableProdMode, provideZoneChangeDetection} from '@angular/core';

import { routes } from './app/app.routes';
import { AppComponent } from './app/app.component';
import {environment} from './environments/environment';
import { bootstrapApplication, } from '@angular/platform-browser';
import {provideRouter } from '@angular/router';
import { provideHttpClient, withXhr } from '@angular/common/http';

export function getBaseUrl() {
    return document.getElementsByTagName('base')[0].href;
}

if (environment.production) {
    enableProdMode();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(withXhr()),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes)]
};


bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));
