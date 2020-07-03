# Project for journaling your observations around stocks

Patience and information gathering leads to better trading moves. Use this site to keep track of your thoughts around stocks, record your transactions, and review them regularly so that you are ready to make the right decisions when the time comes to buy, sell, or keep a share.

# Current Build and Release status

![Build and test](https://github.com/laimis/stock-analysis/workflows/Build%20and%20test/badge.svg)

![Create Release](https://github.com/laimis/stock-analysis/workflows/Create%20Release/badge.svg)

# Implementation

## Frontend

Angular + ASP.NET Core. Nothing fancy, just basic screens to list your portfolio, search for stocks and provide their analysis.

## Backend

ASP.NET core, background tasks for stock price monitoring

## Storage

Supports Postgres or Redis for storing information.

## Deployments

The project is currently deployed to AWS and can be reached via https://www.nightingaletrading.com


## Stock information

Stock information is provided by https://www.iexcloud.io/.
