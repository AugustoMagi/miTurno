import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import {
  cancelarReservaCliente,
  crearReserva,
  getNegocioPublico,
  getReservaCliente,
  getTurnosDisponibles,
} from '../api/negociosPublicos'
import { extractError } from '../api/client'
import { EstadoReserva } from '../types/negocio'
import type { RecursoPublico, Reserva, TurnoDisponible } from '../types/negocio'
import { Card } from '../components/Card'
import { Button } from '../components/Button'
import { Spinner } from '../components/Spinner'
import { ErrorBanner } from '../components/ErrorBanner'
import { Field, Input } from '../components/Input'
import { ArrowLeftIcon, CheckIcon } from '../components/icons'
import { validarEmail, validarRequerido, validarTelefono } from '../utils/validation'

function todayIsoDate(): string {
  const now = new Date()
  const offset = now.getTimezoneOffset()
  return new Date(now.getTime() - offset * 60_000).toISOString().slice(0, 10)
}

function formatHora(horaHms: string): string {
  return horaHms.slice(0, 5)
}

// "08:00:00" es hora en punto, "08:15:00" no: distinguirlas ayuda a ubicarse en una lista larga de
// horarios cada 15 minutos.
function esHoraEnPunto(horaHms: string): boolean {
  return horaHms.slice(3, 5) === '00'
}

export function ReservaWizardPage() {
  const { slug, recursoId } = useParams<{ slug: string; recursoId: string }>()
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()

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
  const [vuelvoDeMercadoPago, setVuelvoDeMercadoPago] = useState(false)

  // Mercado Pago vuelve acá (con reservaId en la URL) después de que el cliente paga. La reserva
  // ya se había creado antes de irse a pagar, así que en vez de mostrar el wizard desde cero,
  // recuperamos su estado actual y saltamos directo a la card de resultado.
  useEffect(() => {
    const reservaId = searchParams.get('reservaId')
    if (!slug || !recursoId || !reservaId) return

    if (searchParams.get('mp') === 'vuelta') {
      setVuelvoDeMercadoPago(true)
      navigate(`/${slug}/reservar/${recursoId}?reservaId=${reservaId}`, { replace: true })
    }

    getReservaCliente(slug, reservaId)
      .then(setReserva)
      .catch((err) => setSubmitError(extractError(err)))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [slug, recursoId])

  // La confirmación del pago llega por webhook de forma asíncrona: si volvemos de Mercado Pago y
  // la reserva sigue Pendiente, sondeamos unas cuantas veces en vez de dejar la pantalla mostrando
  // "pagá de nuevo" para un pago que ya se hizo.
  useEffect(() => {
    const reservaId = searchParams.get('reservaId')
    if (!vuelvoDeMercadoPago || !slug || !reservaId) return

    let cancelado = false
    let intentos = 0

    async function sondear() {
      intentos += 1
      try {
        const actual = await getReservaCliente(slug!, reservaId!)
        if (cancelado) return
        setReserva(actual)
        if (actual.estado !== EstadoReserva.Pendiente) {
          setVuelvoDeMercadoPago(false)
          return
        }
      } catch {
        // error transitorio: seguimos sondeando, no cortamos por esto
      }
      if (!cancelado && intentos < 15) {
        setTimeout(sondear, 4000)
      }
    }

    const timeoutId = setTimeout(sondear, 4000)
    return () => {
      cancelado = true
      clearTimeout(timeoutId)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [vuelvoDeMercadoPago, slug])

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
    const confirmada = !cancelada && reserva.estado === EstadoReserva.Confirmada
    return (
      <div className="animate-fade-in-up mx-auto flex max-w-md flex-col gap-6">
        <div className="relative">
          {confirmada && (
            <>
              <div className="absolute inset-0 translate-x-2 translate-y-3 rotate-2 rounded-xl border-2 border-slate-900/15 bg-white" />
              <span className="absolute -top-4 -right-4 z-10 flex h-16 w-16 rotate-12 items-center justify-center rounded-full border-2 border-dashed border-emerald-600 bg-emerald-50 text-center text-[10px] font-black tracking-wide text-emerald-700 uppercase">
                Confirmado
              </span>
            </>
          )}
        <Card className="relative flex flex-col gap-4">
          <h1 className="text-xl font-semibold text-slate-900">
            {cancelada
              ? 'Reserva cancelada'
              : reserva.estado === EstadoReserva.Confirmada
                ? '¡Reserva confirmada!'
                : reserva.estado === EstadoReserva.Cancelada
                  ? 'El pago no se acreditó'
                  : vuelvoDeMercadoPago
                    ? 'Confirmando tu pago…'
                    : '¡Reserva creada!'}
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
          ) : reserva.estado === EstadoReserva.Confirmada ? (
            <p className="flex items-center gap-1.5 text-sm font-medium text-emerald-700">
              <CheckIcon className="h-4 w-4 shrink-0" />
              Tu pago se acreditó y el turno quedó confirmado. Te esperamos.
            </p>
          ) : reserva.estado === EstadoReserva.Cancelada ? (
            <p className="text-sm text-slate-500">
              El pago no se pudo acreditar y el turno fue liberado. Podés volver a intentar la
              reserva desde el negocio.
            </p>
          ) : vuelvoDeMercadoPago ? (
            <p className="text-sm text-slate-500">
              Estamos confirmando tu pago con Mercado Pago. Puede tardar unos minutos en
              reflejarse acá — no hace falta que vuelvas a pagar.
            </p>
          ) : reserva.linkPago ? (
            <p className="text-sm text-slate-500">
              Confirmá tu turno completando el pago. Una vez acreditado, tu reserva queda
              confirmada automáticamente.
            </p>
          ) : reserva.aliasPago ? (
            <div className="rounded-xl border border-accent-200 bg-accent-50 px-4 py-3 text-sm text-slate-700">
              <p>
                Transferí <span className="font-semibold">${reserva.precioTotal.toLocaleString('es-AR')}</span> a
                este alias para confirmar tu turno:
              </p>
              <p className="mt-1 font-mono text-base font-semibold text-accent-800">
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

          {!cancelada && reserva.estado !== EstadoReserva.Cancelada && reserva.estado !== EstadoReserva.Confirmada && (
            <div className="flex flex-col gap-2 sm:flex-row">
              {reserva.linkPago && (
                <Button className="flex-1" onClick={() => (window.location.href = reserva.linkPago!)}>
                  Pagar con Mercado Pago
                </Button>
              )}
              <Button
                variant="secondary"
                className="flex-1"
                loading={cancelando}
                onClick={handleCancelar}
              >
                Cancelar reserva
              </Button>
            </div>
          )}
        </Card>
        </div>
        <Link to={`/${slug}`} className="text-center text-sm font-medium text-link-600 hover:text-link-700 hover:underline">
          Volver al negocio
        </Link>
      </div>
    )
  }

  return (
    <div className="animate-fade-in-up flex flex-col gap-6">
      <div>
        <Link
          to={`/${slug}`}
          className="inline-flex items-center gap-1 text-sm font-medium text-link-600 hover:text-link-700"
        >
          <ArrowLeftIcon className="h-4 w-4" />
          Volver
        </Link>
        <h1 className="mt-2 text-xl font-semibold text-slate-900 sm:text-2xl">{recurso.nombre}</h1>
        <p className="text-sm text-slate-500">
          {recurso.duracionTurnoMinutos} min · ${recurso.precio.toLocaleString('es-AR')}
        </p>
      </div>

      <div className="flex flex-col gap-4 rounded-xl border-2 border-slate-900 bg-white p-6 shadow-soft">
        <Field label="Elegí una fecha">
          <Input
            type="date"
            className="sm:w-56"
            min={todayIsoDate()}
            value={fecha}
            onChange={(event) => setFecha(event.target.value)}
          />
        </Field>

        {turnosLoading ? (
          <Spinner label="Buscando horarios…" />
        ) : turnosError ? (
          <ErrorBanner message={turnosError} />
        ) : turnos.length === 0 ? (
          <p className="text-sm text-slate-500">No hay turnos disponibles ese día.</p>
        ) : turnoSeleccionado && !mostrarListaHorarios ? (
          <div className="flex items-center justify-between rounded-xl border border-accent-200 bg-accent-50 px-4 py-3 text-sm">
            <span className="font-medium text-accent-800">
              Horario elegido: {formatHora(turnoSeleccionado.horaInicio)} - {formatHora(turnoSeleccionado.horaFin)}
            </span>
            <button
              type="button"
              onClick={() => setMostrarListaHorarios(true)}
              className="font-medium text-link-600 hover:text-link-700 hover:underline"
            >
              Cambiar
            </button>
          </div>
        ) : (
          <div className="flex max-h-80 flex-col divide-y divide-slate-200 overflow-y-auto rounded-xl border border-slate-200">
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
                  className={`flex items-center justify-between border-l-4 px-4 py-3 text-left text-sm font-medium transition-colors duration-200 ${
                    seleccionado
                      ? 'border-accent-500 bg-accent-50 text-accent-800'
                      : esHoraEnPunto(turno.horaInicio)
                        ? 'border-transparent bg-slate-50 text-slate-700 hover:bg-slate-100'
                        : 'border-transparent text-slate-700 hover:bg-slate-50'
                  }`}
                >
                  <span>
                    {formatHora(turno.horaInicio)} - {formatHora(turno.horaFin)}
                  </span>
                  {seleccionado && <CheckIcon className="h-4 w-4 text-accent-600" />}
                </button>
              )
            })}
          </div>
        )}
      </div>

      {turnoSeleccionado && (
        <div className="animate-scale-in rounded-xl border-2 border-slate-900 bg-white p-6 shadow-soft">
          <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
            <h2 className="font-semibold text-slate-900">Tus datos</h2>

            <Field label="Nombre" error={tocado.nombre ? errorNombre : undefined} required>
              <Input
                type="text"
                required
                maxLength={150}
                value={clienteNombre}
                onChange={(event) => setClienteNombre(event.target.value)}
                onBlur={() => setTocado((t) => ({ ...t, nombre: true }))}
                aria-invalid={Boolean(tocado.nombre && errorNombre)}
              />
            </Field>

            <Field label="Email" error={tocado.email ? errorEmail : undefined} required>
              <Input
                type="email"
                required
                maxLength={200}
                value={clienteEmail}
                onChange={(event) => setClienteEmail(event.target.value)}
                onBlur={() => setTocado((t) => ({ ...t, email: true }))}
                aria-invalid={Boolean(tocado.email && errorEmail)}
              />
            </Field>

            <Field label="Teléfono (opcional)" error={tocado.telefono ? errorTelefono : undefined}>
              <Input
                type="tel"
                maxLength={30}
                value={clienteTelefono}
                onChange={(event) => setClienteTelefono(event.target.value)}
                onBlur={() => setTocado((t) => ({ ...t, telefono: true }))}
                aria-invalid={Boolean(tocado.telefono && errorTelefono)}
              />
            </Field>

            {submitError && <ErrorBanner message={submitError} />}

            <Button type="submit" disabled={!formularioValido} loading={enviando}>
              Reservar
            </Button>
          </form>
        </div>
      )}
    </div>
  )
}
