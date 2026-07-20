import { useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { loginAdmin } from '../../api/authAdmin'
import { extractError } from '../../api/client'
import { useAdminAuth } from '../../context/AdminAuthContext'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { ErrorBanner } from '../../components/ErrorBanner'

export function AdminLoginPage() {
  const { sesion, login } = useAdminAuth()
  const navigate = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [enviando, setEnviando] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (sesion) {
    return <Navigate to="/admin/planes" replace />
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
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
    <div className="flex min-h-svh items-center justify-center bg-slate-50 px-4">
      <div className="w-full max-w-sm">
        <p className="mb-6 text-center text-xl font-semibold text-slate-900">
          Mi<span className="text-emerald-600">Turno</span> <span className="text-slate-400">Admin</span>
        </p>
        <Card>
          <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
            <h1 className="text-lg font-semibold text-slate-900">Acceso de plataforma</h1>

            <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
              Email
              <input
                type="email"
                required
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
              />
            </label>

            <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
              Contraseña
              <input
                type="password"
                required
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
              />
            </label>

            {error && <ErrorBanner message={error} />}

            <Button type="submit" disabled={enviando}>
              {enviando ? 'Ingresando…' : 'Ingresar'}
            </Button>
          </form>
        </Card>
      </div>
    </div>
  )
}
