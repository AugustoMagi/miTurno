import { useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { loginAdmin } from '../../api/authAdmin'
import { extractError } from '../../api/client'
import { useAdminAuth } from '../../context/AdminAuthContext'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { ErrorBanner } from '../../components/ErrorBanner'
import { Field, Input } from '../../components/Input'
import { LockIcon, MailIcon } from '../../components/icons'
import { validarEmail, validarRequerido } from '../../utils/validation'

export function AdminLoginPage() {
  const { sesion, login } = useAdminAuth()
  const navigate = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [enviando, setEnviando] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [tocado, setTocado] = useState<{ email?: boolean; password?: boolean }>({})

  if (sesion) {
    return <Navigate to="/admin/planes" replace />
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
      const nuevaSesion = await loginAdmin({ email, password })
      login(nuevaSesion)
      navigate('/admin/planes')
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
            Mi<span className="text-accent-500">Turno</span> <span className="text-slate-400">Admin</span>
          </p>
        </div>
        <Card>
          <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
            <h1 className="text-lg font-semibold text-slate-900">Acceso de plataforma</h1>

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

            {error && <ErrorBanner message={error} />}

            <Button type="submit" loading={enviando}>
              Ingresar
            </Button>
          </form>
        </Card>
      </div>
    </div>
  )
}
