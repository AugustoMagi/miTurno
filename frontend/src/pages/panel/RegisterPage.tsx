import { useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { registrar } from '../../api/auth'
import { extractError } from '../../api/client'
import { useAuth } from '../../context/AuthContext'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { ErrorBanner } from '../../components/ErrorBanner'

// Deriva un slug razonable del nombre del negocio; el usuario puede después ajustarlo a mano,
// por eso dejamos de autogenerarlo apenas lo toca (slugTocado).
function slugify(valor: string): string {
  return valor
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
}

export function RegisterPage() {
  const { sesion, login } = useAuth()
  const navigate = useNavigate()

  const [nombreNegocio, setNombreNegocio] = useState('')
  const [slug, setSlug] = useState('')
  const [slugTocado, setSlugTocado] = useState(false)
  const [emailNegocio, setEmailNegocio] = useState('')
  const [nombreUsuario, setNombreUsuario] = useState('')
  const [emailUsuario, setEmailUsuario] = useState('')
  const [password, setPassword] = useState('')
  const [enviando, setEnviando] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (sesion) {
    return <Navigate to="/panel/estadisticas" replace />
  }

  function handleNombreNegocioChange(valor: string) {
    setNombreNegocio(valor)
    if (!slugTocado) setSlug(slugify(valor))
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    setEnviando(true)
    setError(null)
    try {
      const sesionNueva = await registrar({
        nombreNegocio,
        slug,
        emailNegocio,
        nombreUsuario,
        emailUsuario,
        password,
      })
      login(sesionNueva)
      navigate('/panel/estadisticas')
    } catch (err) {
      setError(extractError(err))
    } finally {
      setEnviando(false)
    }
  }

  return (
    <div className="flex min-h-svh items-center justify-center bg-slate-50 px-4 py-10">
      <div className="w-full max-w-sm">
        <p className="mb-6 text-center text-xl font-semibold text-slate-900">
          Mi<span className="text-emerald-600">Turno</span>
        </p>
        <Card>
          <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
            <h1 className="text-lg font-semibold text-slate-900">Creá tu negocio</h1>

            <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
              Nombre del negocio
              <input
                type="text"
                required
                maxLength={150}
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                value={nombreNegocio}
                onChange={(event) => handleNombreNegocioChange(event.target.value)}
              />
            </label>

            <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
              Slug (URL pública)
              <input
                type="text"
                required
                maxLength={100}
                pattern="[a-z0-9\-]+"
                title="Solo minúsculas, números y guiones"
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                value={slug}
                onChange={(event) => {
                  setSlugTocado(true)
                  setSlug(event.target.value)
                }}
              />
              {slug && <span className="text-xs font-normal text-slate-500">miturno.app/{slug}</span>}
            </label>

            <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
              Email del negocio
              <input
                type="email"
                required
                maxLength={200}
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                value={emailNegocio}
                onChange={(event) => setEmailNegocio(event.target.value)}
              />
            </label>

            <hr className="border-slate-200" />

            <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
              Tu nombre
              <input
                type="text"
                required
                maxLength={150}
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                value={nombreUsuario}
                onChange={(event) => setNombreUsuario(event.target.value)}
              />
            </label>

            <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
              Tu email (para ingresar)
              <input
                type="email"
                required
                maxLength={200}
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                value={emailUsuario}
                onChange={(event) => setEmailUsuario(event.target.value)}
              />
            </label>

            <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
              Contraseña
              <input
                type="password"
                required
                minLength={8}
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
              />
              <span className="text-xs font-normal text-slate-500">Mínimo 8 caracteres.</span>
            </label>

            {error && <ErrorBanner message={error} />}

            <Button type="submit" disabled={enviando}>
              {enviando ? 'Creando…' : 'Crear negocio'}
            </Button>

            <p className="text-center text-sm text-slate-600">
              ¿Ya tenés cuenta?{' '}
              <Link to="/panel/login" className="font-medium text-emerald-600 hover:underline">
                Ingresá
              </Link>
            </p>
          </form>
        </Card>
      </div>
    </div>
  )
}
