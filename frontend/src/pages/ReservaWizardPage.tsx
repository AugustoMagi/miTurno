import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { cancelarReservaCliente, crearReserva, getNegocioPublico, getTurnosDisponibles } from '../api/negociosPublicos'
import { extractError } from '../api/client'
import type { RecursoPublico, Reserva, TurnoDisponible } from '../types/negocio'
import { Card } from '../components/Card'
import { Button } from '../components/Button'
import { Spinner } from '../components/Spinner'
import { ErrorBanner } from '../components/ErrorBanner'
import { FieldError } from '../components/FieldError'
import { validarEmail, validarRequerido, validarTelefono } from '../utils/validation'

function todayIsoDate(): string {
  const now = new Date()
  const offset = now.getTimezoneOffset()
  return new Date(now.getTime() - offset * 60_000).toISOString().slice(0, 10)
}

function formatHora(horaHms: string): string {
  return horaHms.slice(0, 5)
}

export function ReservaWizardPage() {
  const { slug, recursoId } = useParams<{ slug: string; recursoId: string }>()

  const [recurso, setRecurso] = useState<RecursoPublico | null>(null)
  const [negocioError, setNegocioError] = useState<string | null>(null)

  const [fecha, setFecha] = useState(todayIsoDate())
  const [turnos, setTurnos] = useState<TurnoDisponible[]>([])
  const [turnosLoading, setTurnosLoading] = useState(false)
  const [turnosError, setTurnosError] = useState<string | null>(null)
  const [turnoSeleccionado, setTurnoSeleccionado] = useState<TurnoDisponible | null>(null)
  const [mostrarListaHorarios, setMostrarListaHorarios] = useState(true)

  const [clienteNombre, setClienteNombre] = useState('')
  const [clienteEmail, setClienteEmail] = useState('')
  const [clienteTelefono, setClienteTelefono] = useState('')
  const [tocado, setTocado] = useState<{ nombre?: boolean; email?: boolean; telefono?: boolean }>({})
  const [enviando, setEnviando] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)

  const [reserva, setReserva] = useState<Reserva | null>(null)
  const [cancelando, setCancelando] = useState(false)
  const [cancelada, setCancelada] = useState(false)

  useEffect(() => {
    if (!slug || !recursoId) return
    getNegocioPublico(slug)
      .then((negocio) => {
        const encontrado = negocio.recursos.find((r) => r.id === recursoId)
        if (!encontrado) {
          setNegocioError('Este recurso no existe o ya no está disponible.')
          return
        }
        setRecurso(encontrado)
      })
      .catch((err) => setNegocioError(extractError(err)))
  }, [slug, recursoId])

  useEffect(() => {
    if (!slug || !recursoId || !fecha) return
    setTurnosLoading(true)
    setTurnosError(null)
    setTurnoSeleccionado(null)
    setMostrarListaHorarios(true)
    getTurnosDisponibles(slug, recursoId, fecha)
      .then(setTurnos)
      .catch((err) => setTurnosError(extractError(err)))
      .finally(() => setTurnosLoading(false))
  }, [slug, recursoId, fecha])

  const errorNombre = validarRequerido(clienteNombre, 'El nombre')
  const errorEmail = validarEmail(clienteEmail)
  const errorTelefono = validarTelefono(clienteTelefono)

  const formularioValido = !errorNombre && !errorEmail && !errorTelefono

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    setTocado({ nombre: true, email: true, telefono: true })
    if (!slug || !recursoId || !turnoSeleccionado || !formularioValido) return
    setEnviando(true)
    setSubmitError(null)
    try {
      const nuevaReserva = await crearReserva(slug, recursoId, {
        fecha,
        horaInicio: turnoSeleccionado.horaInicio,
        clienteNombre: clienteNombre.trim(),
        clienteEmail: clienteEmail.trim(),
        clienteTelefono: clienteTelefono.trim() || undefined,
      })
      setReserva(nuevaReserva)
    } catch (err) {
      setSubmitError(extractError(err))
    } finally {
      setEnviando(false)
    }
  }

  async function handleCancelar() {
    if (!slug || !reserva) return
    setCancelando(true)
    try {
      await cancelarReservaCliente(slug, reserva.id)
      setCancelada(true)
    } catch (err) {
      setSubmitError(extractError(err))
    } finally {
      setCancelando(false)
    }
  }

  if (negocioError) return <ErrorBanner message={negocioError} />
  if (!recurso) return <Spinner label="Cargando…" />

  if (reserva) {
    return (
      <div className="mx-auto flex max-w-md flex-col gap-6">
        <Card className="flex flex-col gap-4">
          <h1 className="text-xl font-semibold text-slate-900">
            {cancelada ? 'Reserva cancelada' : '¡Reserva creada!'}
          </h1>
          <dl className="grid grid-cols-2 gap-y-2 text-sm">
            <dt className="text-slate-500">Recurso</dt>
            <dd className="text-right font-medium text-slate-900">{recurso.nombre}</dd>
            <dt className="text-slate-500">Fecha</dt>
            <dd className="text-right font-medium text-slate-900">{reserva.fecha}</dd>
            <dt className="text-slate-500">Horario</dt>
            <dd className="text-right font-medium text-slate-900">
              {formatHora(reserva.horaInicio)} - {formatHora(reserva.horaFin)}
            </dd>
            <dt className="text-slate-500">Total</dt>
            <dd className="text-right font-medium text-slate-900">
              ${reserva.precioTotal.toLocaleString('es-AR')}
            </dd>
          </dl>

          {cancelada ? (
            <p className="text-sm text-slate-500">Tu reserva fue cancelada correctamente.</p>
          ) : reserva.linkPago ? (
            <p className="text-sm text-slate-500">
              Confirmá tu turno completando el pago. Una vez acreditado, tu reserva queda
              confirmada automáticamente.
            </p>
          ) : reserva.aliasPago ? (
            <div className="rounded-lg bg-emerald-50 px-4 py-3 text-sm text-slate-700">
              <p>
                Transferí <span className="font-semibold">${reserva.precioTotal.toLocaleString('es-AR')}</span> a
                este alias para confirmar tu turno:
              </p>
              <p className="mt-1 font-mono text-base font-semibold text-emerald-800">
                {reserva.aliasPago}
              </p>
              <p className="mt-2 text-slate-500">
                El negocio va a confirmar tu reserva apenas reciba la transferencia.
              </p>
            </div>
          ) : (
            <p className="text-sm text-slate-500">
              Tu reserva quedó pendiente: el negocio se va a poner en contacto para confirmarla.
            </p>
          )}

          {submitError && <ErrorBanner message={submitError} />}

          {!cancelada && (
            <div className="flex flex-col gap-2 sm:flex-row">
              {reserva.linkPago && (
                <Button className="flex-1" onClick={() => (window.location.href = reserva.linkPago!)}>
                  Pagar con Mercado Pago
                </Button>
              )}
              <Button
                variant="secondary"
                className="flex-1"
                disabled={cancelando}
                onClick={handleCancelar}
              >
                {cancelando ? 'Cancelando…' : 'Cancelar reserva'}
              </Button>
            </div>
          )}
        </Card>
        <Link to={`/${slug}`} className="text-center text-sm text-emerald-700 hover:underline">
          Volver al negocio
        </Link>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link to={`/${slug}`} className="text-sm text-emerald-700 hover:underline">
          ← Volver
        </Link>
        <h1 className="mt-2 text-xl font-semibold text-slate-900 sm:text-2xl">{recurso.nombre}</h1>
        <p className="text-sm text-slate-500">
          {recurso.duracionTurnoMinutos} min · ${recurso.precio.toLocaleString('es-AR')}
        </p>
      </div>

      <Card className="flex flex-col gap-4">
        <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
          Elegí una fecha
          <input
            type="date"
            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none sm:w-56"
            min={todayIsoDate()}
            value={fecha}
            onChange={(event) => setFecha(event.target.value)}
          />
        </label>

        {turnosLoading ? (
          <Spinner label="Buscando horarios…" />
        ) : turnosError ? (
          <ErrorBanner message={turnosError} />
        ) : turnos.length === 0 ? (
          <p className="text-sm text-slate-500">No hay turnos disponibles ese día.</p>
        ) : turnoSeleccionado && !mostrarListaHorarios ? (
          <div className="flex items-center justify-between rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm">
            <span className="font-medium text-emerald-800">
              Horario elegido: {formatHora(turnoSeleccionado.horaInicio)} - {formatHora(turnoSeleccionado.horaFin)}
            </span>
            <button
              type="button"
              onClick={() => setMostrarListaHorarios(true)}
              className="font-medium text-emerald-700 hover:underline"
            >
              Cambiar
            </button>
          </div>
        ) : (
          <div className="flex flex-col divide-y divide-slate-200 overflow-hidden rounded-lg border border-slate-200">
            {turnos.map((turno) => {
              const seleccionado = turno.horaInicio === turnoSeleccionado?.horaInicio
              return (
                <button
                  key={turno.horaInicio}
                  type="button"
                  onClick={() => {
                    setTurnoSeleccionado(turno)
                    setMostrarListaHorarios(false)
                  }}
                  className={`flex items-center justify-between border-l-4 px-4 py-3 text-left text-sm font-medium transition-colors ${
                    seleccionado
                      ? 'border-emerald-600 bg-emerald-50 text-emerald-800'
                      : 'border-transparent text-slate-700 hover:bg-slate-50'
                  }`}
                >
                  <span>
                    {formatHora(turno.horaInicio)} - {formatHora(turno.horaFin)}
                  </span>
                  {seleccionado && (
                    <svg
                      viewBox="0 0 20 20"
                      fill="currentColor"
                      className="h-5 w-5 text-emerald-600"
                      aria-hidden="true"
                    >
                      <path
                        fillRule="evenodd"
                        d="M16.704 5.29a1 1 0 010 1.415l-7.4 7.4a1 1 0 01-1.414 0l-3.6-3.6a1 1 0 111.414-1.414l2.893 2.893 6.693-6.693a1 1 0 011.414 0z"
                        clipRule="evenodd"
                      />
                    </svg>
                  )}
                </button>
              )
            })}
          </div>
        )}
      </Card>

      {turnoSeleccionado && (
        <Card>
          <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
            <h2 className="font-semibold text-slate-900">Tus datos</h2>

            <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
              Nombre
              <input
                type="text"
                required
                maxLength={150}
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                value={clienteNombre}
                onChange={(event) => setClienteNombre(event.target.value)}
                onBlur={() => setTocado((t) => ({ ...t, nombre: true }))}
              />
              {tocado.nombre && <FieldError message={errorNombre} />}
            </label>

            <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
              Email
              <input
                type="email"
                required
                maxLength={200}
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                value={clienteEmail}
                onChange={(event) => setClienteEmail(event.target.value)}
                onBlur={() => setTocado((t) => ({ ...t, email: true }))}
              />
              {tocado.email && <FieldError message={errorEmail} />}
            </label>

            <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
              Teléfono (opcional)
              <input
                type="tel"
                maxLength={30}
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                value={clienteTelefono}
                onChange={(event) => setClienteTelefono(event.target.value)}
                onBlur={() => setTocado((t) => ({ ...t, telefono: true }))}
              />
              {tocado.telefono && <FieldError message={errorTelefono} />}
            </label>

            {submitError && <ErrorBanner message={submitError} />}

            <Button type="submit" disabled={!formularioValido || enviando}>
              {enviando ? 'Confirmando…' : 'Reservar'}
            </Button>
          </form>
        </Card>
      )}
    </div>
  )
}
