import { useEffect, useState } from 'react'
import { actualizarMiNegocio, obtenerMiNegocio } from '../../api/miNegocio'
import { extractError } from '../../api/client'
import type { MiNegocio } from '../../types/miNegocio'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'
import { FieldError } from '../../components/FieldError'
import { validarRequerido, validarTelefono } from '../../utils/validation'

export function MiNegocioPage() {
  const [negocio, setNegocio] = useState<MiNegocio | null>(null)
  const [cargaError, setCargaError] = useState<string | null>(null)
  const [copiado, setCopiado] = useState(false)

  const [nombre, setNombre] = useState('')
  const [descripcion, setDescripcion] = useState('')
  const [direccion, setDireccion] = useState('')
  const [telefono, setTelefono] = useState('')
  const [tocado, setTocado] = useState<{ nombre?: boolean; telefono?: boolean }>({})
  const [guardando, setGuardando] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [guardadoOk, setGuardadoOk] = useState(false)

  useEffect(() => {
    obtenerMiNegocio()
      .then((data) => {
        setNegocio(data)
        setNombre(data.nombre)
        setDescripcion(data.descripcion ?? '')
        setDireccion(data.direccion ?? '')
        setTelefono(data.telefono ?? '')
      })
      .catch((err) => setCargaError(extractError(err)))
  }, [])

  const link = negocio ? `${window.location.origin}/${negocio.slug}` : ''

  async function handleCopiar() {
    await navigator.clipboard.writeText(link)
    setCopiado(true)
    setTimeout(() => setCopiado(false), 2000)
  }

  const errorNombre = validarRequerido(nombre, 'El nombre')
  const errorTelefono = validarTelefono(telefono)

  async function handleGuardar(event: React.FormEvent) {
    event.preventDefault()
    setTocado({ nombre: true, telefono: true })
    if (errorNombre || errorTelefono) return
    setGuardando(true)
    setFormError(null)
    setGuardadoOk(false)
    try {
      const actualizado = await actualizarMiNegocio({
        nombre,
        descripcion: descripcion.trim() || undefined,
        direccion: direccion.trim() || undefined,
        telefono: telefono.trim() || undefined,
      })
      setNegocio(actualizado)
      setGuardadoOk(true)
    } catch (err) {
      setFormError(extractError(err))
    } finally {
      setGuardando(false)
    }
  }

  if (cargaError) return <ErrorBanner message={cargaError} />
  if (!negocio) return <Spinner />

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-xl font-semibold text-slate-900">Mi negocio</h1>

      <Card className="flex flex-col gap-3">
        <h2 className="font-semibold text-slate-900">Tu link público</h2>
        <p className="text-sm text-slate-500">
          Compartilo en tu bio de Instagram o donde quieras: acá es donde tus clientes reservan turnos.
        </p>
        <div className="flex flex-wrap items-center gap-2">
          <a
            href={link}
            target="_blank"
            rel="noreferrer"
            className="rounded-lg border border-slate-300 bg-slate-50 px-3 py-2 text-sm font-medium text-emerald-700 hover:underline"
          >
            {link}
          </a>
          <Button type="button" variant="secondary" onClick={handleCopiar}>
            {copiado ? 'Copiado ✓' : 'Copiar link'}
          </Button>
        </div>
      </Card>

      <Card>
        <form className="flex flex-col gap-4" onSubmit={handleGuardar}>
          <h2 className="font-semibold text-slate-900">Datos del negocio</h2>

          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
            Nombre
            <input
              type="text"
              required
              maxLength={150}
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
              value={nombre}
              onChange={(event) => setNombre(event.target.value)}
              onBlur={() => setTocado((t) => ({ ...t, nombre: true }))}
            />
            {tocado.nombre && <FieldError message={errorNombre} />}
          </label>

          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
            Descripción
            <textarea
              maxLength={500}
              rows={3}
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
              value={descripcion}
              onChange={(event) => setDescripcion(event.target.value)}
            />
          </label>

          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
            Dirección
            <input
              type="text"
              maxLength={200}
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
              value={direccion}
              onChange={(event) => setDireccion(event.target.value)}
            />
          </label>

          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
            Teléfono
            <input
              type="tel"
              maxLength={30}
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
              value={telefono}
              onChange={(event) => setTelefono(event.target.value)}
              onBlur={() => setTocado((t) => ({ ...t, telefono: true }))}
            />
            {tocado.telefono && <FieldError message={errorTelefono} />}
          </label>

          <div className="grid grid-cols-1 gap-4 border-t border-slate-100 pt-4 sm:grid-cols-2">
            <div className="flex flex-col gap-1 text-sm">
              <span className="font-medium text-slate-700">Email de contacto</span>
              <span className="text-slate-500">{negocio.email}</span>
            </div>
            <div className="flex flex-col gap-1 text-sm">
              <span className="font-medium text-slate-700">Slug de la URL</span>
              <span className="text-slate-500">{negocio.slug}</span>
            </div>
          </div>
          <p className="-mt-2 text-xs text-slate-400">
            Para cambiar el email de contacto o el slug de tu link, escribinos.
          </p>

          {formError && <ErrorBanner message={formError} />}
          {guardadoOk && <p className="text-sm text-emerald-700">Guardado.</p>}
          <Button type="submit" disabled={guardando} className="self-start">
            {guardando ? 'Guardando…' : 'Guardar cambios'}
          </Button>
        </form>
      </Card>
    </div>
  )
}
