# News Sources

Verified RSS sources allowed for automated fetching (robots.txt reviewed, no AI/scraping restrictions for `User-agent: *`).

| Name | URL | Language | Categories | Fast Source | Notes |
|---|---|---|---|---|---|
| ProPublica | https://feeds.propublica.org/propublica/main | en | general, politics, business | No | Investigative journalism, USA. robots.txt: no `Disallow` rules for `User-agent: *`. |
| Meduza | https://meduza.io/rss/all | ru | general, politics, Russia | Yes | Independent Russia-focused news. robots.txt: only `/insite/` and `/embed/` disallowed — RSS path clear. |
| The Moscow Times | https://www.themoscowtimes.com/rss/news | en | general, politics, Russia | No | English-language Russia news. robots.txt: disallows `/preview/`, `/search/`, UTM params — RSS path clear. |
| NPR News | https://feeds.npr.org/1001/rss.xml | en | general, world, politics | Yes | US public radio. Feed on `feeds.npr.org` — no robots.txt on that subdomain. Main site blocks only named AI bots; `User-agent: *` doesn't block RSS paths. |

## Verification Checklist

Each source was reviewed for:

- [x] RSS feed opens and returns valid XML
- [x] `<title>` present on items/entries
- [x] `<link>` present on items/entries
- [x] `<description>` or `<summary>` present
- [x] `<pubDate>` or `<published>` present (optional but expected)

4 sources verified via HTTP: ProPublica ✓, Meduza ✓, The Moscow Times ✓, NPR News ✓. Politico removed (robots.txt returns 403). AP News was removed earlier (404).
- [x] `robots.txt` reviewed — no `Disallow` targeting automated readers for `User-agent: *`

---

## Opportunity / Research Sources

Sources for opportunity digest: patents, scientific discoveries, research, grants, startups, regulation, and technology trends.

### Patent sources

| Name | URL | Type | Language | Categories | Fast Source | Notes |
|---|---|---|---|---|---|---|
| arXiv cs.AI | https://rss.arxiv.org/rss/cs.AI | rss | en | research, technology, patents | No | AI/ML preprints. Daily Atom feed. robots.txt: `rss.arxiv.org` subdomain has no restrictions; main site disallows `/api/` and `/search/` only. |
| arXiv cs.LG | https://rss.arxiv.org/rss/cs.LG | rss | en | research, technology | No | Machine learning preprints. Same infrastructure and robots.txt as cs.AI. |

### Scientific news & aggregators

| Name | URL | Type | Language | Categories | Fast Source | Notes |
|---|---|---|---|---|---|---|
| ScienceDaily (All) | https://www.sciencedaily.com/rss/all.xml | rss | en | research, science, technology, health | No | 400+ topic RSS feeds. robots.txt: only `/test/` disallowed for `User-agent: *` — RSS path clear. |
| Phys.org | https://phys.org/rss-feed/ | rss | en | research, science, technology | No | Science X Network. robots.txt: disallows `/rss-feed/search/` and `/rss-feed/tags/`; main `/rss-feed/` path is allowed. |
| Nature News | https://www.nature.com/nature.rss | rss | en | research, science | No | Top scientific discoveries. robots.txt: AI bots (GPTBot, ClaudeBot etc.) blocked by name; `User-agent: *` doesn't block `/nature.rss`. |
| MIT News (Research) | https://news.mit.edu/rss/research | rss | en | research, technology, science | No | Official MIT research announcements. robots.txt: disallows admin/user/search paths only — RSS path clear. |

### Technology & startup news

| Name | URL | Type | Language | Categories | Fast Source | Notes |
|---|---|---|---|---|---|---|
| WIRED (Science) | https://www.wired.com/feed/category/science/latest/rss | rss | en | research, technology, science | No | WIRED science section. robots.txt: disallows `/*?`, auth/account, search — feed path has no query params, clear. |
| WIRED (Top Stories) | https://www.wired.com/feed/rss | rss | en | technology, startups | No | WIRED main feed. Same robots.txt as above — feed path clear. |
| TechCrunch Startups | https://techcrunch.com/category/startups/feed/ | rss | en | startups, technology | No | Startup funding rounds. robots.txt: disallows `/wp-admin/`, `/wp-json/`, search — feed path clear. |
| Hacker News (Show HN) | https://hnrss.org/show | rss | en | startups, technology | No | Community-curated "Show HN". robots.txt: no `Disallow` rules at all. |

### Notes on opportunity sources

- All sources are best suited for the **opportunity digest**, not urgent notifications (`Fast source = false`).
- **arXiv** updates once per business day (Mon–Fri); ScienceDaily, Phys.org, Nature, MIT News update continuously.
- **PubMed / NCBI E-utilities** and **Lens.org API** are viable for patent/research queries but require per-query RSS URLs; add them once specific topics are defined.
- WIPO PATENTSCOPE, USPTO PatFT, EurekAlert, CORDIS, Politico removed — returned 404, connection errors, bot blocks, or HTML instead of XML.
