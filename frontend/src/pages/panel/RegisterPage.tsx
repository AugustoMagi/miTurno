import { useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { registrar } from '../../api/auth'
import { extractError } from '../../api/client'
import { useAuth } from '../../context/AuthContext'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { ErrorBanner } from '../../components/ErrorBanner'
import { Field, Input } from '../../components/Input'
import { BuildingIcon, LockIcon, MailIcon, UserIcon } from '../../components/icons'
import { validarEmail, validarPassword, validarRequerido, validarSlug } from '../../utils/validation'

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
  const [tocado, setTocado] = useState<Record<string, boolean>>({})

  if (sesion) {
    return <Navigate to="/panel/estadisticas" replace />
  }

  function handleNombreNegocioChange(valor: string) {
    setNombreNegocio(valor)
    if (!slugTocado) setSlug(slugify(valor))
  }

  const errorNombreNegocio = validarRequerido(nombreNegocio, 'El nombre del negocio')
  const errorSlug = validarSlug(slug)
  const errorEmailNegocio = validarEmail(emailNegocio, 'El email del negocio')
  const errorNombreUsuario = validarRequerido(nombreUsuario, 'Tu nombre')
  const errorEmailUsuario = validarEmail(emailUsuario, 'Tu email')
  const errorPassword = validarPassword(password)

  const formularioValido =
    !errorNombreNegocio &&
    !errorSlug &&
    !errorEmailNegocio &&
    !errorNombreUsuario &&
    !errorEmailUsuario &&
    !errorPassword

  function marcarTodoTocado() {
    setTocado({
      nombreNegocio: true,
      slug: true,
      emailNegocio: true,
      nombreUsuario: true,
      emailUsuario: true,
      password: true,
    })
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    marcarTodoTocado()
    if (!formularioValido) return
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
    <div className="bg-dotted flex min-h-svh items-center justify-center px-4 py-10">
      <div className="animate-fade-in-up w-full max-w-sm">
        <div className="mb-6 flex items-center justify-center gap-2">
          <img src="/logo.png" alt="MiTurno" className="h-9 w-9 rounded-xl object-cover shadow-soft" />
          <p className="text-xl font-bold tracking-tight text-slate-900" style={{ fontFamily: 'var(--font-heading)' }}>
            Mi<span className="text-accent-500">Turno</span>
          </p>
        </div>
        <Card>
          <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
            <h1 className="text-lg font-semibold text-slate-900">Creá tu negocio</h1>

            <Field label="Nombre del negocio" error={tocado.nombreNegocio ? errorNombreNegocio : undefined} required>
              <Input
                type="text"
                required
                maxLength={150}
                icon={<BuildingIcon />}
                value={nombreNegocio}
                onChange={(event) => handleNombreNegocioChange(event.target.value)}
                onBlur={() => setTocado((t) => ({ ...t, nombreNegocio: true }))}
                aria-invalid={Boolean(tocado.nombreNegocio && errorNombreNegocio)}
              />
            </Field>

            <Field
              label="Slug (URL pública)"
              error={tocado.slug ? errorSlug : undefined}
              hint={slug ? `www.miturno.fun/${slug}` : undefined}
              required
            >
              <Input
                type="text"
                required
                maxLength={100}
                pattern="[a-z0-9\-]+"
                title="Solo minúsculas, números y guiones"
                value={slug}
                onChange={(event) => {
                  setSlugTocado(true)
                  setSlug(event.target.value)
                }}
                onBlur={() => setTocado((t) => ({ ...t, slug: true }))}
                aria-invalid={Boolean(tocado.slug && errorSlug)}
              />
            </Field>

            <Field label="Email del negocio" error={tocado.emailNegocio ? errorEmailNegocio : undefined} required>
              <Input
                type="email"
                required
                maxLength={200}
                icon={<MailIcon />}
                value={emailNegocio}
                onChange={(event) => setEmailNegocio(event.target.value)}
                onBlur={() => setTocado((t) => ({ ...t, emailNegocio: true }))}
                aria-invalid={Boolean(tocado.emailNegocio && errorEmailNegocio)}
              />
            </Field>

            <hr className="border-slate-200" />

            <Field label="Tu nombre" error={tocado.nombreUsuario ? errorNombreUsuario : undefined} required>
              <Input
                type="text"
                required
                maxLength={150}
                icon={<UserIcon />}
                value={nombreUsuario}
                onChange={(event) => setNombreUsuario(event.target.value)}
                onBlur={() => setTocado((t) => ({ ...t, nombreUsuario: true }))}
                aria-invalid={Boolean(tocado.nombreUsuario && errorNombreUsuario)}
              />
            </Field>

            <Field label="Tu email (para ingresar)" error={tocado.emailUsuario ? errorEmailUsuario : undefined} required>
              <Input
                type="email"
                required
                maxLength={200}
                icon={<MailIcon />}
                value={emailUsuario}
                onChange={(event) => setEmailUsuario(event.target.value)}
                onBlur={() => setTocado((t) => ({ ...t, emailUsuario: true }))}
                aria-invalid={Boolean(tocado.emailUsuario && errorEmailUsuario)}
              />
            </Field>

            <Field
              label="Contraseña"
              error={tocado.password ? errorPassword : undefined}
              hint={!tocado.password ? 'Mínimo 8 caracteres.' : undefined}
              required
            >
              <Input
                type="password"
                required
                minLength={8}
                icon={<LockIcon />}
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                onBlur={() => setTocado((t) => ({ ...t, password: true }))}
                aria-invalid={Boolean(tocado.password && errorPassword)}
              />
            </Field>

            {error && <ErrorBanner message={error} />}

            <Button type="submit" loading={enviando}>
              Crear negocio
            </Button>

            <p className="text-center text-sm text-slate-600">
              ¿Ya tenés cuenta?{' '}
              <Link to="/panel/login" className="font-medium text-link-600 hover:text-link-700 hover:underline">
                Ingresá
              </Link>
            </p>
          </form>
        </Card>
      </div>
    </div>
  )
}
