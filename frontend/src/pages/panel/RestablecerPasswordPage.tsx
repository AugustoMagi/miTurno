import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { restablecerPassword } from '../../api/auth'
import { extractError } from '../../api/client'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { ErrorBanner } from '../../components/ErrorBanner'
import { Field, Input } from '../../components/Input'
import { LockIcon } from '../../components/icons'
import { validarConfirmacionPassword, validarPassword } from '../../utils/validation'

export function RestablecerPasswordPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const token = searchParams.get('token')

  const [passwordNueva, setPasswordNueva] = useState('')
  const [passwordConfirmar, setPasswordConfirmar] = useState('')
  const [tocado, setTocado] = useState<{ nueva?: boolean; confirmar?: boolean }>({})
  const [enviando, setEnviando] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const errorPasswordNueva = validarPassword(passwordNueva)
  const errorPasswordConfirmar = validarConfirmacionPassword(passwordNueva, passwordConfirmar)

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    setTocado({ nueva: true, confirmar: true })
    if (errorPasswordNueva || errorPasswordConfirmar || !token) return
    setEnviando(true)
    setError(null)
    try {
      await restablecerPassword(token, passwordNueva)
      navigate('/panel/login', { state: { passwordRestablecida: true } })
    } catch (err) {
      setError(extractError(err))
    } finally {
      setEnviando(false)
    }
  }

  return (
    <div className="bg-dotted flex min-h-svh items-center justify-center px-4">
      <div className="animate-fade-in-up w-full max-w-sm">
        <div className="mb-6 flex items-center justify-center gap-2">
          <img src="/logo.png" alt="MiTurno" className="h-9 w-9 rounded-xl object-cover shadow-soft" />
          <p className="text-xl font-bold tracking-tight text-slate-900" style={{ fontFamily: 'var(--font-heading)' }}>
            Mi<span className="text-accent-500">Turno</span>
          </p>
        </div>
        <Card>
          {!token ? (
            <div className="flex flex-col gap-4">
              <h1 className="text-lg font-semibold text-slate-900">Enlace inválido</h1>
              <p className="text-sm text-slate-600">
                Este link no es válido. Pedí uno nuevo desde la pantalla de ingreso.
              </p>
              <Link
                to="/panel/olvide-password"
                className="text-center text-sm font-medium text-link-600 hover:text-link-700 hover:underline"
              >
                Pedir un link nuevo
              </Link>
            </div>
          ) : (
            <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
              <h1 className="text-lg font-semibold text-slate-900">Elegí una contraseña nueva</h1>

              <Field label="Contraseña nueva" error={tocado.nueva ? errorPasswordNueva : undefined} required>
                <Input
                  type="password"
                  required
                  minLength={8}
                  icon={<LockIcon />}
                  value={passwordNueva}
                  onChange={(event) => setPasswordNueva(event.target.value)}
                  onBlur={() => setTocado((t) => ({ ...t, nueva: true }))}
                  aria-invalid={Boolean(tocado.nueva && errorPasswordNueva)}
                />
              </Field>

              <Field label="Repetir contraseña nueva" error={tocado.confirmar ? errorPasswordConfirmar : undefined} required>
                <Input
                  type="password"
                  required
                  minLength={8}
                  icon={<LockIcon />}
                  value={passwordConfirmar}
                  onChange={(event) => setPasswordConfirmar(event.target.value)}
                  onBlur={() => setTocado((t) => ({ ...t, confirmar: true }))}
                  aria-invalid={Boolean(tocado.confirmar && errorPasswordConfirmar)}
                />
              </Field>

              {error && <ErrorBanner message={error} />}

              <Button type="submit" loading={enviando}>
                Restablecer contraseña
              </Button>
            </form>
          )}
        </Card>
      </div>
    </div>
  )
}
