import { useState } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { login as loginRequest } from '../../api/auth'
import { extractError } from '../../api/client'
import { useAuth } from '../../context/AuthContext'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { ErrorBanner } from '../../components/ErrorBanner'
import { Field, Input } from '../../components/Input'
import { ArrowLeftIcon, CheckIcon, LockIcon, MailIcon } from '../../components/icons'
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
    <div className="bg-dotted flex min-h-svh items-center justify-center px-4">
      <div className="animate-fade-in-up w-full max-w-sm">
        <Link
          to="/"
          className="mb-4 flex items-center justify-center gap-1 text-sm font-medium text-link-600 hover:text-link-700"
        >
          <ArrowLeftIcon className="h-4 w-4" />
          Volver al inicio
        </Link>
        <div className="mb-6 flex items-center justify-center gap-2">
          <img src="/logo.png" alt="MiTurno" className="h-9 w-9 rounded-xl object-cover shadow-soft" />
          <p className="text-xl font-bold tracking-tight text-slate-900" style={{ fontFamily: 'var(--font-heading)' }}>
            Mi<span className="text-accent-500">Turno</span>
          </p>
        </div>
        <Card>
          <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
            <h1 className="text-lg font-semibold text-slate-900">Ingresá a tu panel</h1>

            {passwordRestablecida && (
              <p className="flex items-center gap-2 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
                <CheckIcon className="h-4 w-4 shrink-0" />
                Tu contraseña se actualizó. Ingresá con la nueva.
              </p>
            )}

            <Field label="Email" error={tocado.email ? errorEmail : undefined} required>
              <Input
                type="email"
                required
                icon={<MailIcon />}
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                onBlur={() => setTocado((t) => ({ ...t, email: true }))}
                aria-invalid={Boolean(tocado.email && errorEmail)}
              />
            </Field>

            <Field label="Contraseña" error={tocado.password ? errorPassword : undefined} required>
              <Input
                type="password"
                required
                icon={<LockIcon />}
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                onBlur={() => setTocado((t) => ({ ...t, password: true }))}
                aria-invalid={Boolean(tocado.password && errorPassword)}
              />
            </Field>

            <Link to="/panel/olvide-password" className="text-right text-sm font-medium text-link-600 hover:text-link-700 hover:underline">
              ¿Olvidaste tu contraseña?
            </Link>

            {error && <ErrorBanner message={error} />}

            <Button type="submit" loading={enviando}>
              Ingresar
            </Button>

            <p className="text-center text-sm text-slate-600">
              ¿Todavía no tenés negocio?{' '}
              <Link to="/panel/registro" className="font-medium text-link-600 hover:text-link-700 hover:underline">
                Creá tu cuenta
              </Link>
            </p>
          </form>
        </Card>
      </div>
    </div>
  )
}
