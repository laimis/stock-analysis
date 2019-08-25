# Playground implementation of stock tracking site

This repo contains a simple website one could use to track their stock portfolio.

# Components

## Frontend

Angular + asp.net core for UI. Nothing fancy, just basic screens to list your portfolio, search for stocks and provide their analysis.
Stock details contain charts powered by Google Charts.

## Domain

The project uses event sourcing for tracking owned stocks, this has been a great exercise to try out different event approaches.

## Stock analysis

Using Akka to create components that can inspect a large batch of stocks and find ones that meet a certain criteria. For instance,
find all stocks under $5 that have book value greater than that and a P/E less than 20. That sort of analysis. Results are stored in database
and can be reviewed.

## Storage

Using postgresql for events and analysis results

## Stock information

Stock information is provided by https://financialmodelingprep.com.
