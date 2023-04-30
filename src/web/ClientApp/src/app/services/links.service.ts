import { StockList } from "./stocks.service"
  
export function stockLists_getAnalysisLink(list:StockList) {
    var paramList = list.tickers.map(t => t.ticker).join(',')
    return `/reports/outcomes?tickers=${paramList}&title=${list.name}`
}

export function stockLists_getExportLink(list:StockList, justTickers:boolean = true) {
    var url = `api/portfolio/stocklists/${list.name}/export`

    if (justTickers) {
        url += '?justTickers=true'
    }

    return url
}

export function charts_getTradingViewLink(ticker:string) {
    return `https://www.tradingview.com/chart/kQn4rgoA/?symbol=${ticker}`
}