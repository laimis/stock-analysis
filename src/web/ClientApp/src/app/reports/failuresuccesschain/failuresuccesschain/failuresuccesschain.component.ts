import { Component, OnInit } from '@angular/core';
import {Chain, StocksService} from 'src/app/services/stocks.service';

@Component({
  selector: 'app-failuresuccesschain',
  templateUrl: './failuresuccesschain.component.html',
  styleUrls: ['./failuresuccesschain.component.css']
})
export class FailuresuccesschainComponent implements OnInit {
  chain: Chain;
  render: string = "all";

  constructor(private service : StocksService) { }

  toggle(identifier:string) {
    this.render = identifier
  }

  ngOnInit(): void {
    this.service.chainReport().subscribe( result => {
      this.chain = result
    }, error => {
      console.log("failed: " + error);
		})
  }
  
    showAllLinks() {
        this.render = "all"
    }
    
    showSuccessLinks() {
        this.render = "success"
    }
  
    showFailureLinks() {
        this.render = "failure"
    }

}
