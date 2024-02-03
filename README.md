Gateway
For weather 
- Get coordinates of a city, call to the weather api to get that - weather/coordinates/{city}"
- Get weather per city for the next 5 days - weather/forecast/{city}  (needs gateway to be active to test)
- Get weather per city right now - weather/{city} 
- Choose the warmest day without rain in the next 5 days - weather/warmestday/{city}

For football
- Get venues by city - football/venues/{city}
- Get all the matches in the Premier League on the next 5 days - football/getmatches
- Find a match between 2 teams in the next 5 days - football/getmatch/{home}/{away} 
- Get all matches on a certain day - football/getmatches/{day}
  
Combining 
- Combines the match that the user is requesting by teams, and it suggests to the user whether to go to the match based on the weather that day - football/plan/{home}/{away}
- Get the warmest day in the next 5 days in the specific city and suggest matches taking place in that city, if there are none, suggest other matches in UK on that day. - plan/warmest/{city}
- Get the day you want, and choose the warmest city and match in that city - plan/{date}
