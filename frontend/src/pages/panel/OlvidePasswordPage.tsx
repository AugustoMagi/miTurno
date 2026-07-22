import { useState } from 'react'
import { Link } from 'react-router-dom'
import { solicitarReseteoPassword } from '../../api/auth'
import { extractError } from '../../api/client'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { ErrorBanner } from '../../components/ErrorBanner'
import { FieldError } from '../../components/FieldError'
import { validarEmail } from '../../utils/validation'

export function OlvidePasswordPage() {
  const [email, setEmail] = useState('')
  const [tocado, setTocado] = useState(false)
  const [enviando, setEnviando] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [enviado, setEnviado] = useState(false)

  const errorEmail = validarEmail(email)

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    setTocado(true)
    if (errorEmail) return
    setEnviando(true)
    setError(null)
    try {
      await solicitarReseteoPassword(email)
      setEnviado(true)
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
          {enviado ? (
            <div className="flex flex-col gap-4">
              <h1 className="text-lg font-semibold text-slate-900">Revisá tu email</h1>
              <p className="text-sm text-slate-600">
                Si <span className="font-medium">{email}</span> está registrado, te enviamos un link para
                restablecer tu contraseña. Vence en 30 minutos.
              </p>
              <Link to="/panel/login" className="text-center text-sm font-medium text-emerald-600 hover:underline">
                Volver a ingresar
              </Link>
            </div>
          ) : (
            <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
              <h1 className="text-lg font-semibold text-slate-900">¿Olvidaste tu contraseña?</h1>
              <p className="text-sm text-slate-500">
                Ingresá tu email y te mandamos un link para elegir una contraseña nueva.
              </p>

              <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
                Email
                <input
                  type="email"
                  required
                  className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  onBlur={() => setTocado(true)}
                />
                {tocado && <FieldError message={errorEmail} />}
              </label>

              {error && <ErrorBanner message={error} />}

              <Button type="submit" disabled={enviando}>
                {enviando ? 'Enviando…' : 'Enviar link'}
              </Button>

              <Link to="/panel/login" className="text-center text-sm text-emerald-600 hover:underline">
                Volver a ingresar
              </Link>
            </form>
          )}
        </Card>
      </div>
    </div>
  )
}
