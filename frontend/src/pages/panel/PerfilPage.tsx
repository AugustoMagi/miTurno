import { useEffect, useState } from 'react'
import { actualizarMiPerfil, cambiarMiPassword, obtenerMiPerfil } from '../../api/perfil'
import { extractError } from '../../api/client'
import { useAuth } from '../../context/AuthContext'
import type { MiPerfil } from '../../types/perfil'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'
import { Field, Input } from '../../components/Input'
import { CheckIcon, LockIcon, MailIcon, UserIcon } from '../../components/icons'
import { validarEmail, validarPassword, validarRequerido } from '../../utils/validation'

export function PerfilPage() {
  const { sesion, login } = useAuth()

  const [perfil, setPerfil] = useState<MiPerfil | null>(null)
  const [cargaError, setCargaError] = useState<string | null>(null)

  const [nombre, setNombre] = useState('')
  const [email, setEmail] = useState('')
  const [guardando, setGuardando] = useState(false)
  const [datosError, setDatosError] = useState<string | null>(null)
  const [datosOk, setDatosOk] = useState(false)
  const [datosTocado, setDatosTocado] = useState<{ nombre?: boolean; email?: boolean }>({})

  const [passwordActual, setPasswordActual] = useState('')
  const [passwordNueva, setPasswordNueva] = useState('')
  const [passwordConfirmar, setPasswordConfirmar] = useState('')
  const [cambiandoPassword, setCambiandoPassword] = useState(false)
  const [passwordError, setPasswordError] = useState<string | null>(null)
  const [passwordOk, setPasswordOk] = useState(false)
  const [passwordTocado, setPasswordTocado] = useState<{
    actual?: boolean
    nueva?: boolean
    confirmar?: boolean
  }>({})

  useEffect(() => {
    obtenerMiPerfil()
      .then((data) => {
        setPerfil(data)
        setNombre(data.nombre)
        setEmail(data.email)
      })
      .catch((err) => setCargaError(extractError(err)))
  }, [])

  const errorNombre = validarRequerido(nombre, 'El nombre')
  const errorEmail = validarEmail(email)

  async function handleGuardarDatos(event: React.FormEvent) {
    event.preventDefault()
    setDatosTocado({ nombre: true, email: true })
    if (errorNombre || errorEmail) return
    setGuardando(true)
    setDatosError(null)
    setDatosOk(false)
    try {
      const actualizado = await actualizarMiPerfil({ nombre, email })
      setPerfil(actualizado)
      if (sesion) login({ ...sesion, nombre: actualizado.nombre, email: actualizado.email })
      setDatosOk(true)
    } catch (err) {
      setDatosError(extractError(err))
    } finally {
      setGuardando(false)
    }
  }

  const errorPasswordActual = validarRequerido(passwordActual, 'La contraseña actual')
  const errorPasswordNueva = validarPassword(passwordNueva)
  const errorPasswordConfirmar =
    !errorPasswordNueva && passwordNueva !== passwordConfirmar
      ? 'Las contraseñas nuevas no coinciden.'
      : undefined

  async function handleCambiarPassword(event: React.FormEvent) {
    event.preventDefault()
    setPasswordTocado({ actual: true, nueva: true, confirmar: true })
    setPasswordError(null)
    setPasswordOk(false)
    if (errorPasswordActual || errorPasswordNueva || errorPasswordConfirmar) return
    if (passwordNueva === passwordActual) {
      setPasswordError('La contraseña nueva debe ser distinta a la actual.')
      return
    }
    setCambiandoPassword(true)
    try {
      await cambiarMiPassword({ passwordActual, passwordNueva })
      setPasswordActual('')
      setPasswordNueva('')
      setPasswordConfirmar('')
      setPasswordOk(true)
    } catch (err) {
      setPasswordError(extractError(err))
    } finally {
      setCambiandoPassword(false)
    }
  }

  if (cargaError) return <ErrorBanner message={cargaError} />
  if (!perfil) return <Spinner />

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-xl font-semibold text-slate-900">Mi perfil</h1>

      <Card className="flex flex-col gap-4">
        <div className="flex items-center gap-2">
          <h2 className="font-semibold text-slate-900">Tus datos</h2>
          <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600">
            {perfil.rol}
          </span>
        </div>
        <form className="flex flex-col gap-4" onSubmit={handleGuardarDatos}>
          <Field label="Nombre" error={datosTocado.nombre ? errorNombre : undefined} required>
            <Input
              type="text"
              required
              maxLength={150}
              icon={<UserIcon />}
              value={nombre}
              onChange={(event) => setNombre(event.target.value)}
              onBlur={() => setDatosTocado((t) => ({ ...t, nombre: true }))}
              aria-invalid={Boolean(datosTocado.nombre && errorNombre)}
            />
          </Field>
          <Field label="Email" error={datosTocado.email ? errorEmail : undefined} required>
            <Input
              type="email"
              required
              maxLength={200}
              icon={<MailIcon />}
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              onBlur={() => setDatosTocado((t) => ({ ...t, email: true }))}
              aria-invalid={Boolean(datosTocado.email && errorEmail)}
            />
          </Field>
          {datosError && <ErrorBanner message={datosError} />}
          {datosOk && (
            <p className="flex items-center gap-1.5 text-sm font-medium text-emerald-700">
              <CheckIcon className="h-4 w-4" />
              Datos actualizados.
            </p>
          )}
          <Button type="submit" loading={guardando} className="self-start">
            Guardar cambios
          </Button>
        </form>
      </Card>

      <Card className="flex flex-col gap-4">
        <h2 className="font-semibold text-slate-900">Cambiar contraseña</h2>
        <form className="flex flex-col gap-4" onSubmit={handleCambiarPassword}>
          <Field label="Contraseña actual" error={passwordTocado.actual ? errorPasswordActual : undefined} required>
            <Input
              type="password"
              required
              icon={<LockIcon />}
              value={passwordActual}
              onChange={(event) => setPasswordActual(event.target.value)}
              onBlur={() => setPasswordTocado((t) => ({ ...t, actual: true }))}
              aria-invalid={Boolean(passwordTocado.actual && errorPasswordActual)}
            />
          </Field>
          <Field label="Contraseña nueva" error={passwordTocado.nueva ? errorPasswordNueva : undefined} required>
            <Input
              type="password"
              required
              minLength={8}
              icon={<LockIcon />}
              value={passwordNueva}
              onChange={(event) => setPasswordNueva(event.target.value)}
              onBlur={() => setPasswordTocado((t) => ({ ...t, nueva: true }))}
              aria-invalid={Boolean(passwordTocado.nueva && errorPasswordNueva)}
            />
          </Field>
          <Field
            label="Repetir contraseña nueva"
            error={passwordTocado.confirmar ? errorPasswordConfirmar : undefined}
            required
          >
            <Input
              type="password"
              required
              minLength={8}
              icon={<LockIcon />}
              value={passwordConfirmar}
              onChange={(event) => setPasswordConfirmar(event.target.value)}
              onBlur={() => setPasswordTocado((t) => ({ ...t, confirmar: true }))}
              aria-invalid={Boolean(passwordTocado.confirmar && errorPasswordConfirmar)}
            />
          </Field>
          {passwordError && <ErrorBanner message={passwordError} />}
          {passwordOk && (
            <p className="flex items-center gap-1.5 text-sm font-medium text-emerald-700">
              <CheckIcon className="h-4 w-4" />
              Contraseña actualizada.
            </p>
          )}
          <Button type="submit" loading={cambiandoPassword} className="self-start">
            Cambiar contraseña
          </Button>
        </form>
      </Card>
    </div>
  )
}
