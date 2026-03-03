namespace finviz

type ScreenerResult = {
    ticker: string
    company: string
    sector: string
    industry: string
    country: string
    marketCap: decimal
    price: decimal
    change: decimal
    volume: int64
}

type EarningsTime =
    | BeforeMarket
    | AfterMarket
