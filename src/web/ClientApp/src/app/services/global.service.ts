import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class GlobalService {
  public customVariable = new BehaviorSubject<any>({
    isLoggedIn: false
  });

  markLoggedIn() {
    this.customVariable.value.isLoggedIn = true
    this.customVariable.next(this.customVariable.value)
  }
}

