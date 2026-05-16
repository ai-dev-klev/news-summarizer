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

### Patent sources

| Name | URL | Type | Language | Categories | Fast Source | Notes |
|---|---|---|---|---|---|---|
| WIPO PATENTSCOPE (PCT) | https://patentscope.wipo.int/search/en/rss.jsf?query=&office=WO | rss | en | patents | No | Weekly PCT international applications. Official WIPO feed; search queries can be appended. `Content` = abstract only. |
| USPTO PatFT (new grants) | https://www.rss.uspto.gov/rss/patft/patft_rss.xml | rss | en | patents | No | Official USPTO RSS for newly granted US patents. No auth, no restrictions. `Content` = null (titles + links only). |
| arXiv cs.AI | https://rss.arxiv.org/rss/cs.AI | rss | en | research, technology, patents | No | AI/ML preprints. Daily Atom feed. Often describes patentable methods before formal filing. |
| arXiv cs.LG | https://rss.arxiv.org/rss/cs.LG | rss | en | research, technology | No | Machine learning preprints. Same infrastructure as cs.AI. |

### Scientific news & aggregators

| Name | URL | Type | Language | Categories | Fast Source | Notes |
|---|---|---|---|---|---|---|
| ScienceDaily (All) | https://www.sciencedaily.com/rss/all.xml | rss | en | research, science, technology, health | No | 400+ topic RSS feeds; `all.xml` covers everything. Title + summary + link. No auth. |
| Phys.org | https://phys.org/rss-feed/ | rss | en | research, science, technology | No | Science X Network main feed. Updates continuously. No ads in feed, no auth. |
| EurekAlert! (AAAS) | https://www.eurekalert.org/rss/all.xml | rss | en | research, science | No | Press releases from universities & research institutes. AAAS-sponsored. |
| Nature News | https://www.nature.com/nature.rss | rss | en | research, science | No | Top scientific discoveries. RSS publicly available. |
| MIT News (Research) | https://news.mit.edu/rss/research | rss | en | research, technology, science | No | Official MIT research announcements. |
| EU CORDIS Research | https://cordis.europa.eu/news/rss.xml | rss | en | research, grants, regulation | No | EU Horizon Europe project news and results. Official EU open data. |

### Technology & startup news

| Name | URL | Type | Language | Categories | Fast Source | Notes |
|---|---|---|---|---|---|---|
| WIRED (Science) | https://www.wired.com/feed/category/science/latest/rss | rss | en | research, technology, science | No | WIRED science section. Updates several times per day. |
| WIRED (Top Stories) | https://www.wired.com/feed/rss | rss | en | technology, startups | No | WIRED main feed. Good for big tech & regulation news. |
| TechCrunch Startups | https://techcrunch.com/category/startups/feed/ | rss | en | startups, technology | No | Startup funding rounds and launches. Standard WordPress RSS. |
| Hacker News (Show HN) | https://hnrss.org/show | rss | en | startups, technology | No | Community-curated "Show HN" — new tools, products, research demos. |

### Notes on opportunity sources

- All sources are best suited for the **opportunity digest**, not urgent notifications (`Fast source = false`).
- **Patent feeds** (WIPO, USPTO) typically provide titles and links only — `Content = null` is expected.
- **arXiv** updates once per business day (Mon–Fri); ScienceDaily, Phys.org, EurekAlert update continuously.
- **WIPO PATENTSCOPE** RSS supports query parameters — you can filter by IPC class or keyword in the URL.
- **PubMed / NCBI E-utilities** and **Lens.org API** are also viable but require per-query RSS URLs; add them once specific research topics are defined.
- CORDIS covers EU Horizon Europe grants and research project results.
