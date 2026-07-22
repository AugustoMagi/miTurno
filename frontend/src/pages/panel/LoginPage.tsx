import { useState } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { login as loginRequest } from '../../api/auth'
import { extractError } from '../../api/client'
import { useAuth } from '../../context/AuthContext'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { ErrorBanner } from '../../components/ErrorBanner'
import { FieldError } from '../../components/FieldError'
import { validarEmail, validarRequerido } from '../../utils/validation'

export function LoginPage() {
  const { sesion, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const passwordRestablecida = Boolean((location.state as { passwordRestablecida?: boolean } | null)?.passwordRestablecida)

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [enviando, setEnviando] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [tocado, setTocado] = useState<{ email?: boolean; password?: boolean }>({})

  if (sesion) {
    return <Navigate to="/panel/estadisticas" replace />
  }

  const errorEmail = validarEmail(email)
  const errorPassword = validarRequerido(password, 'La contraseña')

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    setTocado({ email: true, password: true })
    if (errorEmail || errorPassword) return
    setEnviando(true)
    setError(null)
    try {
      const nuevaSesion = await loginRequest({ email, password })
      login(nuevaSesion)
      navigate('/panel/estadisticas')
    } catch (err) {
      setError(extractError(err))
    } finally {
      setEnviando(false)
    }
  }

  return (
    <div className="flex min-h-svh items-center justify-center bg-slate-50 px-4">
      <div className="w-full max-w-sm">
        <p className="mb-6 text-center text-xl font-semibold text-slate-900">
          Mi<span className="text-emerald-600">Turno</span>
        </p>
        <Card>
          <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
            <h1 className="text-lg font-semibold text-slate-900">Ingresá a tu panel</h1>

            {passwordRestablecida && (
              <p className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
                Tu contraseña se actualizó. Ingresá con la nueva.
              </p>
            )}

            <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
              Email
              <input
                type="email"
                required
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                onBlur={() => setTocado((t) => ({ ...t, email: true }))}
              />
              {tocado.email && <FieldError message={errorEmail} />}
            </label>

            <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
              Contraseña
              <input
                type="password"
                required
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                onBlur={() => setTocado((t) => ({ ...t, password: true }))}
              />
              {tocado.password && <FieldError message={errorPassword} />}
            </label>

            <Link to="/panel/olvide-password" className="text-right text-sm text-emerald-600 hover:underline">
              ¿Olvidaste tu contraseña?
            </Link>

            {error && <ErrorBanner message={error} />}

            <Button type="submit" disabled={enviando}>
              {enviando ? 'Ingresando…' : 'Ingresar'}
            </Button>

            <p className="text-center text-sm text-slate-600">
              ¿Todavía no tenés negocio?{' '}
              <Link to="/panel/registro" className="font-medium text-emerald-600 hover:underline">
                Creá tu cuenta
              </Link>
            </p>
          </form>
        </Card>
      </div>
    </div>
  )
}
