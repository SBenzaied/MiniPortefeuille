---
version: 1
slug: "src-web"
primary_target: "src/web"
related_targets: []
---

# Mini-portefeuille : application web (refonte)

Scope: whole app (login, client list, client sheet, position form, empty/error/loading states). Mode: Operate.
Audience: evaluators first, through a credible advisor tool; advisor at the desk, screen-shared with a client, on tablet/phone.
Constraints: Piguet Galland colour codes (petrol #1C343A/#15292F, peach #F8E7D5/#FDEEE1, lime #DBFC87, stone #D8D7CD, lavender #9589C9, terracotta #D7804C, mint #CEE5DE); no PG logo/name. Light + dark. Neither playful nor dull. Strict CSP: self-hosted fonts only.
Unresolved: none.

## Direction contract

THESIS: Each client reads like an investment-committee page: actual allocation set against the bounds of the risk profile. Refuses the KPI-cards + donut wealth dashboard.

OWN-WORLD: Peach paper (#FDEEE1) under petrol ink, a full-width petrol band for the app header and login, lime only on petrol (primary action, focus). Hairline rules instead of cards; one grotesk, tabular figures. Asset classes: petrol, lavender, mint-deep, stone. Breach is terracotta hatching beyond the band edge.

STORY: The advisor sees at once who is out of profile, opens the client, understands value, allocation and exactly which bound is crossed and by how much, then corrects positions.

FIRST VIEWPORT: Client sheet: header row with client name (large) and profile/advisor line, total value set right on the same baseline with a CHF/EUR/USD segmented switch; below, the committee chart: one row per asset class, shared 0–100 % axis, the profile band tinted, the actual bar to exact scale, overshoot hatched, amount and % at right. Status line under it. Positions table follows; primary action "Ajouter une position" on the table header.

FORM: Investment-committee pack (strategic allocation vs min/max bands); safer-register lineup, user choice; seed key c224b061.

Raises kept from the first round: exact-scale bars on a shared axis; breach legible without colour (hatching); states printed on the sheet, not toasts; one text size for data; destructive action isolated.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
