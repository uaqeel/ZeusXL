# ZeusXL
Zeus addin for Excel

An addin I wrote (and re-wrote and re-wrote and re-wrote) starting 2010. Zeus has, over time, been a backtesting engine, a HFT engine and a powerful utilities library.

In its current incarnation, it is an addin that retrieves/manipulates Bloomberg data through an embedded R interpreter. This R functionality is modelled after ContraModus, a web-based analytics tool created by my former colleague Vladimir Vladimirov. The R functionality not only allows basic data manipulation but sophisticated backtests incorporating vol-targetting, dynamic positioning and much more.

## Retrieving Bloomberg data
You can retrieve end-of-day prices for Bloomberg tickers quite straightforwardly by invoking **ZDH** in Excel:

  =ZDH(Tickers, StartDate, EndDateOpt, FieldOpt, PeriodicityOpt, ReverseOpt, NoLabelsOpt, NamesOpt, NormaliseOpt)
  
  =ZDH("AAPL Equity", 21-Jan-2022, 21-Feb-2022, "PX_BID")
  
Unless specified, FieldOpt will default to "PX_LAST" while EndDateOpt will default to today's date. We default to using the 5-day calendar and non-trading day fill option to standardise the time series and simplify data analysis across multiple tickers.

## Using R to manipulate stock price time series
The **ZEval** function combines the ability to retrieve data for Bloomberg tickers with invokations to R.NET to manipulate it.

  =ZEval(Expressions, StartDate, EndDateOpt, FieldOpt, PeriodicityOpt, ReverseOpt, NoLabelsOpt, NamesOpt)
  
  =ZEval("\*{r@AAPL Equity}", 21-Jan-2022, 21-Feb-2022, "PX_LAST")
