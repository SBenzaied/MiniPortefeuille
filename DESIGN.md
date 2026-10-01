---
name: Mini-portefeuille
description: Outil de conseiller où chaque client se lit comme une page de comité de placement, répartition face aux bornes du profil.
colors:
  paper: "#fdeee1"
  paper-deep: "#f8e7d5"
  paper-raised: "#fff8f1"
  ink: "#1c343a"
  ink-deep: "#15292f"
  ink-soft: "#4b5f63"
  rule: "rgb(28 52 58 / 0.16)"
  rule-strong: "rgb(28 52 58 / 0.42)"
  lime: "#dbfc87"
  band-head: "#1c343a"
  band-head-ink: "#f8e7d5"
  band-head-soft: "#b9c4bf"
  corridor: "rgb(28 52 58 / 0.09)"
  breach: "#a3471f"
  breach-hatch: "#d7804c"
  breach-wash: "rgb(215 128 76 / 0.14)"
  danger: "#a3361f"
  danger-wash: "rgb(163 54 31 / 0.08)"
  series-equities: "#1c343a"
  series-bonds: "#8d80c4"
  series-cash: "#4f9483"
  series-other: "#a9a492"
typography:
  display:
    fontFamily: "'Hanken Grotesk Variable', 'Segoe UI', system-ui, sans-serif"
    fontSize: "2.25rem"
    fontWeight: 600
    lineHeight: 1.15
    letterSpacing: "-0.025em"
    fontFeature: "'tnum' 1, 'lnum' 1"
  display-band:
    fontFamily: "'Hanken Grotesk Variable', 'Segoe UI', system-ui, sans-serif"
    fontSize: "clamp(2rem, 3.4vw, 3rem)"
    fontWeight: 600
    lineHeight: 1.15
    letterSpacing: "-0.03em"
  headline:
    fontFamily: "'Hanken Grotesk Variable', 'Segoe UI', system-ui, sans-serif"
    fontSize: "1.35rem"
    fontWeight: 600
    lineHeight: 1.15
    letterSpacing: "-0.015em"
  title:
    fontFamily: "'Hanken Grotesk Variable', 'Segoe UI', system-ui, sans-serif"
    fontSize: "1.05rem"
    fontWeight: 600
    lineHeight: 1.15
  body:
    fontFamily: "'Hanken Grotesk Variable', 'Segoe UI', system-ui, sans-serif"
    fontSize: "15px"
    fontWeight: 400
    lineHeight: 1.5
    fontFeature: "'tnum' 1, 'lnum' 1"
  label:
    fontFamily: "'Hanken Grotesk Variable', 'Segoe UI', system-ui, sans-serif"
    fontSize: "0.85rem"
    fontWeight: 600
    lineHeight: 1.5
  label-quiet:
    fontFamily: "'Hanken Grotesk Variable', 'Segoe UI', system-ui, sans-serif"
    fontSize: "0.8rem"
    fontWeight: 500
    lineHeight: 1.5
rounded:
  mark: "2px"
  md: "6px"
  pill: "999px"
spacing:
  xs: "0.35rem"
  sm: "0.6rem"
  md: "1rem"
  lg: "1.5rem"
  xl: "2.75rem"
components:
  button-primary:
    backgroundColor: "{colors.ink}"
    textColor: "{colors.lime}"
    typography: "{typography.body}"
    rounded: "{rounded.pill}"
    padding: "0.5rem 1.15rem"
    height: "2.5rem"
  button-primary-hover:
    backgroundColor: "{colors.ink-deep}"
    textColor: "{colors.lime}"
  button-secondary:
    backgroundColor: "transparent"
    textColor: "{colors.ink}"
    rounded: "{rounded.pill}"
    padding: "0.5rem 1.15rem"
    height: "2.5rem"
  button-on-band:
    backgroundColor: "transparent"
    textColor: "{colors.band-head-ink}"
    rounded: "{rounded.pill}"
    padding: "0.3rem 0.95rem"
    height: "2.1rem"
  button-on-band-hover:
    textColor: "{colors.lime}"
  button-delete:
    backgroundColor: "transparent"
    textColor: "{colors.danger}"
    rounded: "{rounded.pill}"
    padding: "0.2rem 0.75rem"
    height: "1.9rem"
  button-delete-hover:
    backgroundColor: "{colors.danger-wash}"
    textColor: "{colors.danger}"
  button-confirm-delete:
    backgroundColor: "{colors.danger}"
    textColor: "{colors.paper}"
    rounded: "{rounded.pill}"
    padding: "0.2rem 0.85rem"
    height: "1.9rem"
  segmented-option:
    backgroundColor: "transparent"
    textColor: "{colors.ink-soft}"
    rounded: "{rounded.pill}"
    padding: "0.25rem 0.85rem"
    height: "2rem"
  segmented-option-selected:
    backgroundColor: "{colors.ink}"
    textColor: "{colors.lime}"
    rounded: "{rounded.pill}"
  profile-tag:
    backgroundColor: "transparent"
    textColor: "{colors.ink}"
    rounded: "{rounded.pill}"
    padding: "0.05rem 0.6rem"
  input:
    backgroundColor: "{colors.paper-raised}"
    textColor: "{colors.ink}"
    typography: "{typography.body}"
    rounded: "{rounded.md}"
    padding: "0.55rem 0.7rem"
    height: "2.6rem"
  app-header:
    backgroundColor: "{colors.band-head}"
    textColor: "{colors.band-head-ink}"
    padding: "0.85rem 1.5rem"
  breach-notes:
    backgroundColor: "{colors.breach-wash}"
    textColor: "{colors.ink}"
    rounded: "{rounded.md}"
    padding: "0.9rem 1.1rem"
  form-error:
    backgroundColor: "{colors.danger-wash}"
    textColor: "{colors.danger}"
    rounded: "{rounded.md}"
    padding: "0.75rem 1rem"
---

# Design System: Mini-portefeuille

## Overview

**Creative North Star: "La page de comité"**

Chaque client se lit comme une page de comité de placement : la répartition réelle posée face aux bornes du profil de risque, à l'échelle exacte, sur un axe commun. Le monde est une encre pétrole sur papier pêche, repris des codes couleurs de Piguet Galland, sans logo ni nom de la banque. Un bandeau pétrole pleine largeur porte l'en-tête de l'application et la moitié gauche de la connexion ; tout le reste est du papier, réglé par des filets.

La densité est celle d'un document de travail : une seule grotesque, chiffres tabulaires et alignés, une taille de texte pour les données, des filets au lieu de cartes. La couleur est rare et toujours porteuse de sens : le citron vert pour l'action principale, une teinte fixe par classe d'actifs, la terre cuite hachurée pour un dépassement. Rien n'alarme : l'écart au profil est chiffré et hachuré, jamais rouge vif.

Le mode sombre inverse le papier : la page devient pétrole profond (`#15292f`), l'encre devient pêche (`#f8e7d5`), et le citron vert passe en remplissage de l'action principale, de la sélection et du focus, puisque toute la page est alors du pétrole. Le système refuse le tableau de bord de gestion de fortune fait de cartes KPI et de donut.

**Key Characteristics:**
- Papier pêche, encre pétrole, bandeau pétrole pleine largeur.
- Filets fins à la place des cartes ; aucune ombre portée.
- Une grotesque (Hanken Grotesk Variable), chiffres tabulaires et alignés partout.
- Barres à l'échelle exacte sur un axe 0–100 % commun, bande du profil teintée, dépassement hachuré.
- Actions en pilule ; champs et messages à 6px ; marques de données à 2px.

## Colors

Une palette à deux pôles, pétrole et pêche, avec un seul accent vif (le citron vert) et une terre cuite réservée aux écarts.

### Primary
- **Encre pétrole** (`ink`): texte courant, titres, filets (en transparence), remplissage du bouton principal et de l'option choisie du sélecteur segmenté, anneau de focus en clair. Sert aussi de teinte de la classe Actions (`series-equities`) et du bandeau (`band-head`).
- **Pétrole profond** (`ink-deep`): survol du bouton principal ; papier de la page en mode sombre.
- **Citron vert** (`lime`): texte du bouton principal et de l'option choisie sur pétrole, survol et focus des boutons du bandeau, barre active de la marque. En sombre : remplissage du bouton principal, sélection de texte, focus, `accent-color` et `caret-color`.

### Secondary
- **Terre cuite de hachure** (`breach-hatch`): hachures à -45° de la part qui sort de la bande du profil, et pastille de légende « Écart à la borne ».
- **Terre cuite foncée** (`breach`): texte de l'état « Hors profil », de la borne franchie et du titre de la note d'écart. Son voile (`breach-wash`) fond la note d'écart et la part hachurée au-delà du maximum.

### Tertiary
- **Lavande** (`series-bonds`), **Menthe profonde** (`series-cash`), **Pierre** (`series-other`): teintes fixes des classes Obligations, Liquidités et Autres. Avec le pétrole des Actions, elles forment la seule série de données du système.
- **Brique** (`danger`): erreurs de formulaire et de chargement, action « Supprimer » et sa confirmation. Son voile (`danger-wash`) fond la ligne en cours de suppression et le bloc d'erreur.

### Neutral
- **Papier pêche** (`paper`): fond de page en clair.
- **Pêche profonde** (`paper-deep`): survol des lignes de tableau ; texte du bandeau (`band-head-ink`).
- **Papier relevé** (`paper-raised`): fond des champs de saisie, seul papier plus clair que la page.
- **Encre adoucie** (`ink-soft`): métadonnées, en-têtes de colonnes, libellés, état « Dans le profil ». Sur le bandeau, son pendant est `band-head-soft`.
- **Filet** (`rule`) et **Filet appuyé** (`rule-strong`): `rule` sépare les lignes ; `rule-strong` ouvre et ferme les sections, souligne les en-têtes de tableau et borde les contrôles secondaires.
- **Couloir** (`corridor`): teinte de la plage autorisée par le profil dans chaque piste.

En sombre, chaque jeton est redéfini sous `prefers-color-scheme: dark` (valeurs dans `.impeccable/design.json`, champ `darkValue`) ; les noms et les rôles ne changent pas.

### Named Rules
**The Lime-on-Petrol Rule.** Le citron vert ne touche jamais le papier pêche. Il vit sur le pétrole : texte du bouton principal en clair, remplissage en sombre où toute la page est pétrole.

**The Fixed Series Rule.** Une teinte fixe par classe d'actifs (Actions pétrole, Obligations lavande, Liquidités menthe profonde, Autres pierre), jamais attribuée selon le rang ou la valeur.

**The Hatched Breach Rule.** Un dépassement se signale par une hachure terre cuite au-delà de la borne, plus la borne et l'écart en texte (`≤ 30 % · +35.75 pts`). La couleur n'est jamais la seule information, et l'écart n'est jamais rouge vif.

## Typography

**Display Font:** Hanken Grotesk Variable (avec 'Segoe UI', system-ui, sans-serif)
**Body Font:** Hanken Grotesk Variable, auto-hébergée via `@fontsource-variable/hanken-grotesk`
**Label/Mono Font:** aucune ; les libellés restent dans la même grotesque.

**Character:** Une grotesque unique, nette et légèrement chaleureuse, réglée en chiffres tabulaires et alignés (`'tnum' 1, 'lnum' 1` sur la racine) pour que les montants s'alignent en colonne. La hiérarchie vient de la taille et du poids (500 et 600), jamais d'une seconde famille.

### Hierarchy
- **Display** (600, 2.25rem, 1.15, -0.025em): nom du client et valeur totale (cette dernière à interligne 1), sur la même ligne de base. 1.85rem sous 560px.
- **Display du bandeau** (600, clamp(2rem, 3.4vw, 3rem), -0.03em): phrase d'accroche de la connexion seulement, sur le bandeau.
- **Headline** (600, 1.35rem, 1.15, -0.015em): titres de section (« Répartition face au profil prudent », « Positions »). Le titre du formulaire de connexion monte à 1.5rem.
- **Title** (600, 1.05rem): titre du formulaire de position, nom de l'application dans le bandeau.
- **Body** (400, 15px, 1.5): texte courant et toutes les cellules de données. Les valeurs fortes passent en 600, sans changer de taille.
- **Label** (600, 0.85rem): libellés de champs. Les petites mentions (`.small`, étiquette de profil, erreurs de champ) restent à 0.85rem en 400 ou 500.
- **Label discret** (500, 0.8rem, `ink-soft`): en-têtes de colonnes et graduations de l'axe. Casse de phrase, sans majuscules ni espacement élargi.

### Named Rules
**The One Data Size Rule.** Toutes les données (montants, parts, quantités, cellules) sont à la taille du corps, 15px ; l'emphase passe par le poids 600, jamais par une taille plus grande dans le tableau.

**The Tabular Figures Rule.** Les chiffres tabulaires et alignés sont activés à la racine et ne sont jamais désactivés ; les nombres s'alignent à droite.

## Layout

Une seule colonne centrée de 1180px (`--measure`), marges de 1.5rem (1rem sous 560px), espace de page `2rem 1.5rem 4rem`. Le bandeau pétrole va bord à bord, son contenu suit la même mesure.

La fiche client se compose en bandes horizontales réglées : en-tête de page (nom à gauche, valeur totale et sélecteur de devise à droite, alignés en bas, fermé par un filet appuyé), puis la section de répartition, puis les positions. Les sections s'espacent de 2.75rem au-dessus de leur titre et 1rem dessous. La grille de répartition aligne cinq colonnes (classe, piste, profil, montant, part) avec un écart de 1.25rem ; la piste occupe la plus large.

Le rythme d'espacement se lit en `0.35rem / 0.6rem / 1rem / 1.5rem / 2.75rem`. Les lignes de tableau ont 0.85rem de marge verticale et pas de marge latérale aux extrémités : le texte s'aligne sur les filets.

Responsive : à 860px, chaque ligne de répartition s'empile (classe et part, piste, borne et montant), la valeur totale passe à gauche, le formulaire passe à deux colonnes et la connexion à une colonne sans spécimen. À 640px, les tableaux deviennent des lignes empilées (nom et valeur, puis détails en `ink-soft`, puis actions). À 560px, le titre réduit et le formulaire passe à une colonne.

La connexion partage l'écran en deux : bandeau pétrole (marque, accroche, spécimen de pistes, mention « données fictives ») à gauche, formulaire sur papier à droite, centré, 22rem de large.

## Elevation & Depth

Le système est plat. Aucune ombre portée : la profondeur vient du ton (bandeau pétrole sur papier, papier relevé des champs, voiles terre cuite et brique) et des filets. `box-shadow` n'apparaît qu'en filet intérieur de 1 à 1.5px, pour tracer les bornes du couloir et le contour des hachures.

### Shadow Vocabulary
- **Borne du couloir** (`box-shadow: inset 1.5px 0 0 var(--corridor-edge)` et/ou `inset -1.5px 0 0 var(--corridor-edge)`): bornes minimum et maximum de la plage du profil.
- **Contour de hachure** (`box-shadow: inset 0 0 0 1px var(--breach-hatch)`): cadre de la part hachurée et de sa pastille de légende.

### Named Rules
**The Rules-Not-Cards Rule.** Une section s'ouvre par un filet appuyé et ses lignes se séparent par des filets fins ; aucun contenu n'est posé sur une carte bordée ou ombrée. Seuls les messages (note d'écart, erreur) reçoivent un voile teinté sans bordure.

## Shapes

Trois rayons, chacun avec son rôle. La pilule (999px) pour tout ce qui s'actionne ou se choisit : boutons, sélecteur segmenté, étiquette de profil, bouton de suppression. 6px pour ce qui contient du texte saisi ou un message : champs, note d'écart, bloc d'erreur. 2px pour les marques de données : pastilles de série, pastille de hachure, bout droit des barres (le côté gauche reste carré, ancré sur l'axe). Les filets font 1px ; les bornes du couloir 1.5px. Les icônes sont dessinées sur une grille de 16, trait 1.5, extrémités arrondies.

La marque de l'application (et le favicon) reprend le motif du graphique : trois barres, dont une citron vert dans une bande autorisée en voile citron, dans un carré à coins arrondis.

## Components

### Buttons
Actions en pilule, sobres et franches.
- **Shape:** pilule (999px), hauteur minimale 2.5rem, icône de 1.05em avec un écart de 0.45rem.
- **Primary:** pétrole plein, texte citron vert, poids 600 ; une seule par zone (« Ajouter une position », « Enregistrer », « Se connecter »). En sombre : citron vert plein, texte pétrole profond.
- **Hover / Focus:** le fond passe à `ink-deep` (en sombre `#e8fdb0`) en 160ms `cubic-bezier(0.16, 1, 0.3, 1)` ; focus en contour de 2px décalé de 2px. Désactivé : opacité 0.5.
- **Secondary:** transparent, texte encre, bord `rule-strong` qui devient encre au survol (« Exporter en CSV », « Annuler »).
- **Sur le bandeau:** transparent, texte pêche, bord pêche à 35 %, poids 500 ; au survol et au focus, bord et texte passent au citron vert.
- **Texte:** « Modifier » est un lien souligné en 600, soulignement `rule-strong` qui prend la couleur du texte au survol.

### Chips
- **Style:** étiquette de profil en pilule, bord `rule-strong`, texte encre 0.85rem en 500, sans fond.
- **State:** statique ; elle nomme le profil, elle ne filtre pas.

### Sélecteur segmenté
Choix de devise et filtre par profil. Une pilule bordée `rule-strong` avec 3px de marge intérieure ; options en `ink-soft` 500, l'option choisie en pétrole plein et citron vert 600 (en sombre, citron vert plein). La légende reste lisible par les lecteurs d'écran et s'affiche à côté en `ink-soft`.

### Inputs / Fields
- **Style:** papier relevé, bord 1px `rule-strong`, rayon 6px, hauteur minimale 2.6rem ; les listes déroulantes portent un chevron dessiné de 16px.
- **Focus:** bord encre et contour de 2px (`--focus`) décalé de 1px ; le survol fait passer le bord à l'encre.
- **Error / Disabled:** bord brique sur `aria-invalid`, message brique 0.85rem en 500 sous le champ ; erreur de formulaire au-dessus avec l'icône d'alerte.

### Navigation
Le bandeau pétrole : marque et nom à gauche (600, 1.05rem), nom du conseiller en pêche et bouton de déconnexion sur le bandeau à droite. Sous 560px, le nom du conseiller disparaît. Le retour (« Mes clients ») est un lien `ink-soft` 500 avec flèche, qui passe à l'encre au survol.

### Tableaux
Pleine largeur, ouverts par un filet appuyé, lignes séparées par `rule`, en-têtes en label discret avec indicateur de tri dessiné (opacité 0.55, 1 sur la colonne triée, qui passe aussi en 600). Survol de ligne en `paper-deep` en 120ms. Nombres à droite. La ligne de total est en 600, sous un filet appuyé. La suppression se confirme dans la ligne, qui prend le voile brique ; « Supprimer » reste isolé à droite, en brique et sans bord jusqu'au survol.

### Répartition face au profil (signature)
Une ligne par classe : pastille de série et nom, piste, borne du profil, montant, part en 600. La piste porte une ligne de base `rule-strong`, des repères à 25, 50 et 75 % en `rule`, le couloir du profil teinté avec ses bornes de 1.5px, la barre de 0.75rem à l'échelle exacte dans la teinte de la classe, et la part hors bande hachurée en terre cuite. Les barres se déploient depuis la gauche en 720ms avec 70ms de décalage par ligne, la hachure suit 380ms plus tard ; rien ne bouge si `prefers-reduced-motion`. Sous le graphique : la note d'écart (voile terre cuite, 6px, titre terre cuite avec icône d'alerte), puis la légende des deux pastilles et la mention des taux fixes.

## Do's and Don'ts

### Do:
- **Do** poser la couleur sur le pétrole ou le papier selon The Lime-on-Petrol Rule : citron vert sur pétrole uniquement.
- **Do** séparer les contenus par des filets (`rule` entre les lignes, `rule-strong` pour ouvrir une section) plutôt que par des cartes.
- **Do** garder toutes les données à 15px en chiffres tabulaires, alignées à droite, l'emphase en poids 600.
- **Do** accompagner tout état coloré d'un texte et d'une forme : hachure et écart chiffré pour un dépassement, couloir miniature pour « Dans le profil ».
- **Do** utiliser la pilule pour les actions et les choix, 6px pour les champs et les messages, 2px pour les marques de données.
- **Do** dessiner les icônes sur la grille de 16, trait 1.5 en `currentColor`.
- **Do** redéfinir un jeton pour le mode sombre plutôt que d'écrire une valeur en dur.

### Don't:
- **Don't** construire un tableau de bord en cartes KPI et donut ; la répartition se lit en barres à l'échelle exacte sur un axe commun.
- **Don't** poser le citron vert sur le papier pêche, en texte comme en fond.
- **Don't** ajouter d'ombre portée ni de carte bordée ; la profondeur reste tonale.
- **Don't** attribuer les couleurs de série selon le rang, ni introduire une cinquième teinte de données.
- **Don't** signaler un écart en rouge vif ou en vert « OK » ; l'écart est terre cuite hachurée, la conformité reste en `ink-soft`.
- **Don't** ajouter le logo ou le nom Piguet Galland ; seuls les codes couleurs sont repris.
- **Don't** introduire une seconde famille de caractères ni une police servie depuis un domaine externe.
- **Don't** mettre de surtitre en majuscules au-dessus des titres ; les libellés restent en casse de phrase.
