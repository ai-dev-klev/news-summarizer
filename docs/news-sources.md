# News Sources

Verified RSS sources allowed for automated fetching (robots.txt reviewed, no AI/scraping restrictions for `User-agent: *`).

| Name | URL | Language | Categories | Fast Source | Notes |
|---|---|---|---|---|---|
| ProPublica | https://feeds.propublica.org/propublica/main | en | general, politics, business | No | Investigative journalism, USA. `Disallow:` is empty. |
| Meduza | https://meduza.io/rss/all | ru | general, politics, Russia | Yes | Independent Russia-focused news. Only technical paths disallowed. |
| The Moscow Times | https://www.themoscowtimes.com/rss/news | en | general, politics, Russia | No | English-language Russia news. Only service paths disallowed. |
| NPR News | https://feeds.npr.org/1001/rss.xml | en | general, world, politics | Yes | US public radio, world news. `feeds.npr.org` has no robots.txt; main site only blocks named AI bots by name for `User-agent: *`. |
| Politico | https://www.politico.com/rss/politicopicks.xml | en | politics | No | US and European politics. Only service sections disallowed, no scraping rules. |

## Verification Checklist

Each source was reviewed for:

- [x] RSS feed opens and returns valid XML
- [x] `<title>` present on items/entries
- [x] `<link>` present on items/entries
- [x] `<description>` or `<summary>` present
- [x] `<pubDate>` or `<published>` present (optional but expected)

All 5 sources verified via HTTP: ProPublica ✓, Meduza ✓, The Moscow Times ✓, NPR News ✓, Politico ✓. AP News (`/rss`) returned 404 and was replaced with NPR News.
- [ ] `robots.txt` reviewed — no `Disallow` targeting automated readers for `User-agent: *`
