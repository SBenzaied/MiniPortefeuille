// Régénère les captures d'écran du README à partir de l'application lancée localement.
// Usage : BASE_URL=http://localhost:8080 node scripts/screenshots.mjs
// Prérequis : `npm i --no-save playwright-core` et un Chrome ou Edge installé.
import { mkdir } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import { chromium } from 'playwright-core'

const baseUrl = process.env.BASE_URL ?? 'http://localhost:8080'
const outDir = new URL('../../../docs/screenshots/', import.meta.url)
await mkdir(outDir, { recursive: true })

const browser = await chromium.launch({ channel: process.env.BROWSER_CHANNEL ?? 'msedge' })
const page = await browser.newPage({ viewport: { width: 1280, height: 860 }, deviceScaleFactor: 1, colorScheme: 'light' })
const shot = (name, options = {}) => page.screenshot({ path: fileURLToPath(new URL(name, outDir)), ...options })

await page.goto(`${baseUrl}/login`)
await page.getByLabel('Identifiant').fill('amartin')
await page.getByLabel('Mot de passe').fill('Demo-2026!')
await shot('01-connexion.png')

await page.getByRole('button', { name: 'Se connecter' }).click()
await page.getByRole('link', { name: 'Jean Rochat' }).waitFor()
await shot('02-liste-clients.png')

await page.getByRole('link', { name: 'Jean Rochat' }).click()
await page.getByText('Valeur totale').waitFor()
await page.waitForTimeout(300)
await shot('03-fiche-client.png', { fullPage: true })

await page.getByRole('button', { name: 'Ajouter une position' }).click()
await page.getByLabel('Quantité').fill('-5')
await page.getByLabel('Devise', { exact: true }).selectOption('USD')
await page.getByRole('button', { name: 'Enregistrer' }).click()
await page.getByText('La quantité doit être strictement positive.').waitFor()
// Le formulaire n'a pas de cadre : on capture sa zone avec une marge autour.
const box = await page.locator('.position-form').boundingBox()
const margin = 24
await page.screenshot({
  path: fileURLToPath(new URL('04-erreurs-validation.png', outDir)),
  fullPage: true,
  clip: { x: box.x - margin, y: box.y - margin, width: box.width + 2 * margin, height: box.height + 2 * margin },
})

await browser.close()
console.log(`Captures écrites dans ${fileURLToPath(outDir)}`)
