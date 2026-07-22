import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { restablecerPassword } from '../../api/auth'
import { extractError } from '../../api/client'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { ErrorBanner } from '../../components/ErrorBanner'
import { FieldError } from '../../components/FieldError'
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
    <div className="flex min-h-svh items-center justify-center bg-slate-50 px-4">
      <div className="w-full max-w-sm">
        <p className="mb-6 text-center text-xl font-semibold text-slate-900">
          Mi<span className="text-emerald-600">Turno</span>
        </p>
        <Card>
          {!token ? (
            <div className="flex flex-col gap-4">
              <h1 className="text-lg font-semibold text-slate-900">Enlace inválido</h1>
              <p className="text-sm text-slate-600">
                Este link no es válido. Pedí uno nuevo desde la pantalla de ingreso.
              </p>
              <Link
                to="/panel/olvide-password"
                className="text-center text-sm font-medium text-emerald-600 hover:underline"
              >
                Pedir un link nuevo
              </Link>
            </div>
          ) : (
            <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
              <h1 className="text-lg font-semibold text-slate-900">Elegí una contraseña nueva</h1>

              <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
                Contraseña nueva
                <input
                  type="password"
                  required
                  minLength={8}
                  className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                  value={passwordNueva}
                  onChange={(event) => setPasswordNueva(event.target.value)}
                  onBlur={() => setTocado((t) => ({ ...t, nueva: true }))}
                />
                {tocado.nueva && <FieldError message={errorPasswordNueva} />}
              </label>

              <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
                Repetir contraseña nueva
                <input
                  type="password"
                  required
                  minLength={8}
                  className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                  value={passwordConfirmar}
                  onChange={(event) => setPasswordConfirmar(event.target.value)}
                  onBlur={() => setTocado((t) => ({ ...t, confirmar: true }))}
                />
                {tocado.confirmar && <FieldError message={errorPasswordConfirmar} />}
              </label>

              {error && <ErrorBanner message={error} />}

              <Button type="submit" disabled={enviando}>
                {enviando ? 'Guardando…' : 'Restablecer contraseña'}
              </Button>
            </form>
          )}
        </Card>
      </div>
    </div>
  )
}
