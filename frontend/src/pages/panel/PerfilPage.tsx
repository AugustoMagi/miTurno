import { useEffect, useState } from 'react'
import { actualizarMiPerfil, cambiarMiPassword, obtenerMiPerfil } from '../../api/perfil'
import { extractError } from '../../api/client'
import { useAuth } from '../../context/AuthContext'
import type { MiPerfil } from '../../types/perfil'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'
import { FieldError } from '../../components/FieldError'
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
          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
            Nombre
            <input
              type="text"
              required
              maxLength={150}
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
              value={nombre}
              onChange={(event) => setNombre(event.target.value)}
              onBlur={() => setDatosTocado((t) => ({ ...t, nombre: true }))}
            />
            {datosTocado.nombre && <FieldError message={errorNombre} />}
          </label>
          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
            Email
            <input
              type="email"
              required
              maxLength={200}
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              onBlur={() => setDatosTocado((t) => ({ ...t, email: true }))}
            />
            {datosTocado.email && <FieldError message={errorEmail} />}
          </label>
          {datosError && <ErrorBanner message={datosError} />}
          {datosOk && <p className="text-sm text-emerald-700">Datos actualizados.</p>}
          <Button type="submit" disabled={guardando} className="self-start">
            {guardando ? 'Guardando…' : 'Guardar cambios'}
          </Button>
        </form>
      </Card>

      <Card className="flex flex-col gap-4">
        <h2 className="font-semibold text-slate-900">Cambiar contraseña</h2>
        <form className="flex flex-col gap-4" onSubmit={handleCambiarPassword}>
          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
            Contraseña actual
            <input
              type="password"
              required
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
              value={passwordActual}
              onChange={(event) => setPasswordActual(event.target.value)}
              onBlur={() => setPasswordTocado((t) => ({ ...t, actual: true }))}
            />
            {passwordTocado.actual && <FieldError message={errorPasswordActual} />}
          </label>
          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
            Contraseña nueva
            <input
              type="password"
              required
              minLength={8}
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
              value={passwordNueva}
              onChange={(event) => setPasswordNueva(event.target.value)}
              onBlur={() => setPasswordTocado((t) => ({ ...t, nueva: true }))}
            />
            {passwordTocado.nueva && <FieldError message={errorPasswordNueva} />}
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
              onBlur={() => setPasswordTocado((t) => ({ ...t, confirmar: true }))}
            />
            {passwordTocado.confirmar && <FieldError message={errorPasswordConfirmar} />}
          </label>
          {passwordError && <ErrorBanner message={passwordError} />}
          {passwordOk && <p className="text-sm text-emerald-700">Contraseña actualizada.</p>}
          <Button type="submit" disabled={cambiandoPassword} className="self-start">
            {cambiandoPassword ? 'Guardando…' : 'Cambiar contraseña'}
          </Button>
        </form>
      </Card>
    </div>
  )
}
