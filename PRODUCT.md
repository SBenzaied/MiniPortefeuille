# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

**Public prioritaire : les évaluateurs de l'exercice technique.** Mini-portefeuille est un exercice
réalisé pour le poste de Développeur IT (.NET / React) chez Piguet Galland. Recruteurs et tech leads
ouvrent l'application (souvent via `docker compose up`, avec les comptes de démonstration), lisent le
README et le code. Ils jugent la crédibilité du résultat : est-ce qu'un vrai conseiller pourrait s'en
servir, et est-ce que le candidat a la rigueur attendue dans une banque privée ?

**Utilisateur simulé : le conseiller en patrimoine.** Il consulte le portefeuille de ses clients,
repère ceux dont la répartition s'écarte du profil de risque et gère leurs positions. Trois
situations confirmées :

- seul au bureau, sur un poste de travail, pour préparer un rendez-vous ou surveiller les alertes ;
- en rendez-vous, écran partagé avec le client : ce qui s'affiche doit être présentable au client ;
- en déplacement, sur mobile ou tablette.

## Product Purpose

Permettre à un conseiller de consulter rapidement, pour chacun de ses clients, la valeur totale du
portefeuille dans une devise de référence (CHF par défaut), sa répartition par classe d'actifs en
montant et en pourcentage, et d'être alerté quand cette répartition sort des bornes du profil de
risque. Il peut aussi ajouter, modifier et supprimer des positions, avec une validation claire.

Pour l'exercice, le succès se mesure en deux temps. D'abord, les évaluateurs trouvent l'application
juste (calculs, cloisonnement, erreurs) et soignée. Ensuite, elle démontre la maîtrise de .NET et
React, la sécurité et la qualité d'ingénierie.

## Positioning

L'application n'est pas un produit commercial. Ce qui la distingue, c'est l'exactitude et la
transparence. Les montants sont calculés en `decimal` et arrondis une seule fois, à la fin. Les
alertes se comparent au pourcentage arrondi affiché. Le cloisonnement par conseiller passe par le
jeton (404 et non 403). Le README documente chaque choix et chaque limite. L'interface doit refléter
ce même sérieux : des chiffres fiables, présentés avec la sobriété d'une banque privée suisse.

## Operating Context

- Connexion avec un compte conseiller. Deux comptes de démonstration : `amartin` (4 clients, dont
  2 en alerte) et `bnguyen` (3 clients, dont un sans position).
- Parcours : connexion → « Mes clients » (filtre par profil conservé dans l'URL, nombre d'alertes)
  → fiche client (valeur totale, répartition face aux bornes du profil avec écart chiffré, alerte,
  tableau des positions triable, export CSV) → formulaire de position (erreurs de l'API affichées sous
  chaque champ).
- Les évaluateurs lancent l'application via Docker Compose (http://localhost:8080) ou en développement
  (Vite, http://localhost:5173).
- Les captures du README sont générées par Playwright (`src/web/scripts/screenshots.mjs`) : tout
  changement visuel implique de les régénérer.

## Capabilities and Constraints

- Interface en français. Format suisse `de-CH` partout : `CHF 48'330.00`, `65.75 %`, y compris dans
  les messages d'alerte de l'API.
- Profils de risque : Prudent, Équilibré, Dynamique. Classes d'actifs : Actions, Obligations,
  Liquidités, Autres. Les bornes d'alerte sont dans `src/Portfolio.Domain/Alerts/AllocationPolicy.cs`.
- Front React 19 + TypeScript (Vite), fiche client chargée à la demande, état local aux pages, pas
  de librairie d'état ni de graphiques. Servi par nginx avec une CSP stricte en conteneur : pas de
  ressource externe (polices auto-hébergées).
- La couleur n'est jamais la seule information : chaque ligne de répartition porte aussi sa borne,
  son écart, son montant et sa part en texte.
- Limites assumées : taux de change fixes, prix saisis à la main, pas d'historique, journal d'audit
  sans écran de consultation, pas de contrôle de concurrence.

## Brand Commitments

- Nom du produit : **Mini-portefeuille**.
- **Reprendre les codes couleurs de Piguet Galland** (https://www.piguetgalland.ch/), décision de
  l'utilisateur. Les valeurs exactes restent à relever sur le site lors du travail visuel.
- Non décidé : l'usage du logo ou du nom Piguet Galland dans l'interface. Ne pas l'ajouter sans
  confirmation.
- Ton : factuel, précis et sobre, comme le README (phrases courtes, aucun superlatif).

## Evidence on Hand

- Jeu d'essai fictif chargé au démarrage (conseillers, clients, positions, taux). Toutes les données
  sont fictives.
- Captures : `docs/screenshots/01-connexion.png`, `02-liste-clients.png`, `03-fiche-client.png`,
  `04-erreurs-validation.png`.
- Valorisation de Jean Rochat recalculée à la main et figée dans un test d'intégration.
- Aucun témoignage, client réel, chiffre d'usage ni donnée de marché : ne pas en inventer.

## Product Principles

1. **Les chiffres d'abord, et justes.** Valeur, répartition et écart au profil se lisent sans effort.
   Les montants respectent le format suisse et ne sont jamais approximés à l'affichage.
2. **L'alerte se voit, sans alarmer.** Un écart au profil est une information à traiter avec le
   client, pas une panique. Elle doit être visible, précise et chiffrée.
3. **Présentable devant le client.** Tout écran peut être partagé en rendez-vous : aucune donnée d'un
   autre client, aucun jargon technique, aucune erreur brute.
4. **La rigueur se voit dans les détails.** Pour les évaluateurs, la qualité perçue vient de la
   cohérence : formats, états vides, chargement, erreurs, accessibilité.
5. **Honnête sur ses limites.** Les données sont fictives et les taux fixes : l'interface le dit, et
   ne prétend pas faire plus que ce qu'elle fait.

## Accessibility & Inclusion

Aucune norme formelle imposée. Exigences établies dans le projet : la couleur n'est jamais la seule
information, et les erreurs de formulaire sont annoncées (`role="alert"`) et associées à leur champ.
L'interface doit fonctionner sur poste de travail, tablette et mobile.
