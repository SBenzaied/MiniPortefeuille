import { useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/http'
import { useAuth } from '../auth/AuthContext'
import { BrandMark, Icon } from '../components/Icon'

// Illustration du principe de l'outil : une répartition lue face à la plage autorisée du profil.
const SPECIMEN = [
  { label: 'Actions', share: 66, min: 0, max: 30, color: '#f8e7d5' },
  { label: 'Obligations', share: 21, min: 0, max: 100, color: '#aaa0de' },
  { label: 'Liquidités', share: 13, min: 0, max: 100, color: '#7fc2ad' },
]

export function LoginPage() {
  const { advisor, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const from = (location.state as { from?: string } | null)?.from ?? '/clients'
  if (advisor) return <Navigate to={from} replace />

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      await login(userName, password)
      navigate(from, { replace: true })
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Une erreur inattendue est survenue.')
      setSubmitting(false)
    }
  }

  return (
    <div className="login-page">
      <div className="login-band">
        <span className="brand">
          <BrandMark />
          Mini-portefeuille
        </span>
        <div className="login-pitch">
          <h1>Le portefeuille de chaque client, face à son profil.</h1>
          <p>Valeur totale, répartition par classe d'actifs et écarts au profil de risque, en un coup d'œil.</p>
        </div>
        <div className="login-specimen" aria-hidden="true">
          {SPECIMEN.map((s) => (
            <div key={s.label} className="login-specimen-row">
              <span>{s.label}</span>
              <div className="track">
                {s.max < 100 && <span className="corridor has-max" style={{ left: `${s.min}%`, width: `${s.max - s.min}%` }} />}
                <span className="bar" style={{ width: `${s.share}%`, background: s.color }} />
                {s.share > s.max && <span className="overshoot over" style={{ left: `${s.max}%`, width: `${s.share - s.max}%` }} />}
              </div>
            </div>
          ))}
        </div>
        <p className="login-footnote">Exercice technique · toutes les données sont fictives.</p>
      </div>

      <div className="login-form-wrap">
        <form className="login-form" onSubmit={handleSubmit}>
          <h2>Espace conseiller</h2>
          <p className="muted">Connectez-vous pour consulter vos clients.</p>

          {error && (
            <div className="form-error" role="alert">
              <Icon name="alert" />
              {error}
            </div>
          )}

          <div className="field">
            <label htmlFor="userName">Identifiant</label>
            <input
              id="userName"
              value={userName}
              onChange={(e) => setUserName(e.target.value)}
              autoComplete="username"
              required
              autoFocus
            />
          </div>
          <div className="field">
            <label htmlFor="password">Mot de passe</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="current-password"
              required
            />
          </div>
          <button type="submit" className="button" disabled={submitting}>
            {submitting ? 'Connexion…' : 'Se connecter'}
          </button>
        </form>
      </div>
    </div>
  )
}
