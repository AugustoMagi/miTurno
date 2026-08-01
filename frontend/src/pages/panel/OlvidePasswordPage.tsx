import { useState } from 'react'
import { Link } from 'react-router-dom'
import { solicitarReseteoPassword } from '../../api/auth'
import { extractError } from '../../api/client'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { ErrorBanner } from '../../components/ErrorBanner'
import { Field, Input } from '../../components/Input'
import { ArrowLeftIcon, MailIcon } from '../../components/icons'
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
          {enviado ? (
            <div className="flex flex-col gap-4">
              <h1 className="text-lg font-semibold text-slate-900">Revisá tu email</h1>
              <p className="text-sm text-slate-600">
                Si <span className="font-medium">{email}</span> está registrado, te enviamos un link para
                restablecer tu contraseña. Vence en 30 minutos.
              </p>
              <Link to="/panel/login" className="text-center text-sm font-medium text-link-600 hover:text-link-700 hover:underline">
                Volver a ingresar
              </Link>
            </div>
          ) : (
            <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
              <h1 className="text-lg font-semibold text-slate-900">¿Olvidaste tu contraseña?</h1>
              <p className="text-sm text-slate-500">
                Ingresá tu email y te mandamos un link para elegir una contraseña nueva.
              </p>

              <Field label="Email" error={tocado ? errorEmail : undefined} required>
                <Input
                  type="email"
                  required
                  icon={<MailIcon />}
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  onBlur={() => setTocado(true)}
                  aria-invalid={Boolean(tocado && errorEmail)}
                />
              </Field>

              {error && <ErrorBanner message={error} />}

              <Button type="submit" loading={enviando}>
                Enviar link
              </Button>

              <Link to="/panel/login" className="text-center text-sm font-medium text-link-600 hover:text-link-700 hover:underline">
                Volver a ingresar
              </Link>
            </form>
          )}
        </Card>
      </div>
    </div>
  )
}
