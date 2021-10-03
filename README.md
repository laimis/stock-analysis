# Project for tracking stock, option, and crypto currency buys and sells

The site allows to track stock, option, and crypto currency purchases. For storage it uses either redis or postgres, for stock information IEX API (https://iexcloud.io/docs/api/), and for crypto CoinMarketCap API (https://coinmarketcap.com/api). For both you will need your own key when running the project.
# Current Build and Release status

![Build and test](https://github.com/laimis/stock-analysis/workflows/Build%20and%20test/badge.svg)

![Create Release](https://github.com/laimis/stock-analysis/workflows/Create%20Release/badge.svg)

# Implementation

## Frontend

Angular + ASP.NET Core. Nothing fancy, just basic screens to list your portfolio, search for stocks and provide their analysis.

## Backend

ASP.NET core, background tasks for stock price monitoring and 30 day sell reminders (avoid wash rule).

## Storage

Supports Postgres or Redis for storing information.

## Deployments

The project is currently deployed to AWS and can be reached via https://www.nightingaletrading.com


## Stock information

Stock information is provided by https://www.iexcloud.io/.
