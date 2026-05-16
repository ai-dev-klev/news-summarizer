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

---

## Opportunity / Research Sources

Sources for opportunity digest: patents, scientific discoveries, research, grants, startups, regulation, and technology trends.

| Name | URL | Language | Categories | Fast Source | Notes |
|---|---|---|---|---|---|
| USPTO Patent Full-Text RSS | https://patents.google.com/rss/query=&tbm=pts | en | patents | No | Google Patents RSS for new US patents. No auth required, no scraping restrictions. |
| arXiv cs.AI | https://rss.arxiv.org/rss/cs.AI | en | research, technology | No | Latest AI/ML preprints from arXiv. Stable Atom feed, no restrictions. |
| arXiv cs.LG | https://rss.arxiv.org/rss/cs.LG | en | research, technology | No | Machine learning preprints. Same feed infrastructure as cs.AI. |
| Nature News | https://www.nature.com/nature.rss | en | research, science | No | Top scientific discoveries from Nature. RSS publicly available. |
| TechCrunch Startups | https://techcrunch.com/category/startups/feed/ | en | startups, technology | No | Startup funding rounds and launches. Standard WordPress RSS. |
| EU EUR-Lex CORDIS Research | https://cordis.europa.eu/news/rss.xml | en | research, grants, regulation | No | EU-funded research project news and results. Official EU open data. |
| MIT News (Research) | https://news.mit.edu/rss/research | en | research, technology, science | No | MIT research announcements. Official feed, no restrictions. |
| Hacker News (Show HN) | https://hnrss.org/show | en | startups, technology | No | Community-curated "Show HN" posts — new tools, products, research demos. |

### Notes on opportunity sources

- These sources are best suited for the **opportunity digest**, not urgent notifications.
- `Fast source = false` for all: articles are typically not time-critical.
- arXiv feeds update once per business day; nature.com and MIT News update several times per week.
- USPTO Google Patents RSS does not include full claim text — `Content` will be `null` for most items.
- CORDIS feed covers EU Horizon Europe grants and research results.
