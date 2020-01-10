# Playground implementation of stock tracking site

This repo contains a simple website one could use to track their stock/options portfolio. It's mostly geared towards analysing option prices and selling options.

# Components

## Frontend

Angular + asp.net core for UI. Nothing fancy, just basic screens to list your portfolio, search for stocks and provide their analysis.
Stock details contain charts powered by Google Charts.

## Domain

The project uses event sourcing for tracking owned stocks, this has been a great exercise to try out different event approaches.

## Storage

Using postgresql for event storage.

## Stock information

Stock information is provided by https://financialmodelingprep.com and https://www.iexcloud.io/
