require(quantmod)
require(PerformanceAnalytics)
require(zoo)


percentage_returns <- function(x) {
  a = c(0, diff(x)) / lag(x)
  a[is.na(a)] <- 0
  return(a)
}

normal_returns <- function(x) {
  c(0, diff(x))
}

log_returns <- function(x) {
  c(0,diff(log(x)))
}

cumprod <- function(a) {
  b = ave(a, is.finite(a), FUN=.Primitive("cumprod"))
  for (i in 1:length(b))
    if(is.na(b[i])) b[i] = 1 else break
  return(b)
}

cumprod2 <- function(returns_vector, n = 1, initial_level = 1) {
  base = rep(initial_level,length(returns_vector))
  levels = rep(initial_level,length(returns_vector))
  for (i in 1:length(returns_vector)) {
    if (!is.na(returns_vector[i])) {
	  if (i == 1)
	    levels[i] = initial_level + returns_vector[i] * initial_level
	  else
	    levels[i] = levels[i-1] + returns_vector[i] * base[i-1]

	  if (i %% n == 0) {
	    base[i] = levels[i]
	  }
    }
  }
  return(levels)
}

cond <- function(test, yes, no) {
  ifelse(test, yes, no)
}

sma <- function(x, n) {
  s = c(rep(NA, n-1), rollmean(x, n))
  return(s)
}

ewma <- function(x, n) {
  ratio = 2 / (n+1)

  l = length(x)
  ret = c(NA,l)
  ret[1] = x[1]

  for (i in 2:l) {
    ret[i] = ratio * x[i] + (1-ratio) * ret[i-1]
  }

  return(ret)
}

sd2 <- function (x, na.rm = FALSE) {
  sqrt(var(if (is.vector(x)) x else as.double(x), na.rm = TRUE))
}

sd <- function(x, n = 63) {
  s = c(rep(NA, n-1), rollapply(x, n, FUN=sd2))
  for (i in 2:length(s))
    if(is.na(s[i])) s[i] = s[max(0,i-1)]
  return(s)
}

lag <- function(x, n = 1) {
  c(NA,head(x,-n))
}

mround <- function(x,base) { 
        base*round(x/base) 
}

kor <- function(x) cor(x)[3]

corr <- function(x, y, n) {
  s = c(rep(NA,n-1),rollapply(cbind(x, y), n, kor, by.column = FALSE))
  return(s)
}

beta <- function(x, y, n) {
  c = corr(x, y, n)
  v = sd(y, n) / sd(x, n)
  b = c * v
  return(b)
}