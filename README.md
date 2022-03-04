# Project for tracking stock, option, and crypto currency buys and sells

The site allows to track stock, option, and crypto currency purchases. For storage it can use redis, postgres, or in-memory storage. BE AWARE: in-memory storage is cleared on each restart.

Stock information is fetched using IEX API (https://iexcloud.io/docs/api/), and for crypto CoinMarketCap API (https://coinmarketcap.com/api) is used. For both you will need your own key when running the project.
# Current Build and Release status

![Build and test](https://github.com/laimis/stock-analysis/workflows/Build%20and%20test/badge.svg)

![Create Release](https://github.com/laimis/stock-analysis/workflows/Create%20Release/badge.svg)

# Implementation

## Frontend

Angular + ASP.NET Core. Nothing fancy, just basic screens to list your portfolio, search for stocks and provide their analysis.

## Backend

ASP.NET core, background tasks for stock price monitoring and 30 day sell reminders (avoid wash rule).

## Storage

Supports Postgres, Redis, In-Memory for storing information.

## Deployments

Use Dockerfile to build a docker image that can then be run locally, on AWS, DigitalOcean, etc.

## Stock information

Stock information is provided by https://www.iexcloud.io/.
