import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import axios from 'axios'
import {
  cambiarPlanMiSuscripcion,
  cancelarMiSuscripcion,
  elegirPlan,
  iniciarSuscripcionMercadoPago,
  obtenerMiSuscripcion,
} from '../../api/miSuscripcion'
import { listarPlanesPublicos } from '../../api/planesPublicos'
import { extractError } from '../../api/client'
import { Periodicidad } from '../../types/plan'
import { EstadoSuscripcion } from '../../types/suscripcionAdmin'
import type { MiSuscripcion } from '../../types/miSuscripcion'
import type { PlanPublico } from '../../types/planPublico'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'

const ESTADO_LABEL: Record<EstadoSuscripcion, string> = {
  [EstadoSuscripcion.EnPrueba]: 'En prueba',
  [EstadoSuscripcion.Activa]: 'Activa',
  [EstadoSuscripcion.Vencida]: 'Vencida',
  [EstadoSuscripcion.Cancelada]: 'Cancelada',
}

const PERIODICIDAD_LABEL: Record<Periodicidad, string> = {
  [Periodicidad.Mensual]: 'mensual',
  [Periodicidad.Anual]: 'anual',
}

function diasRestantes(fechaProximoVencimiento: string): number {
  const ms = new Date(fechaProximoVencimiento).getTime() - Date.now()
  return Math.ceil(ms / (1000 * 60 * 60 * 24))
}

function TextoVencimiento({ suscripcion }: { suscripcion: MiSuscripcion }) {
  const dias = diasRestantes(suscripcion.fechaProximoVencimiento)
  const fecha = suscripcion.fechaProximoVencimiento.slice(0, 10)

  if (suscripcion.estado === EstadoSuscripcion.Cancelada) {
    return <p className="text-sm text-slate-500">Vencía el {fecha}.</p>
  }
  if (dias < 0) {
    return <p className="text-sm text-red-600">Venció el {fecha}.</p>
  }
  if (dias === 0) {
    return <p className="text-sm text-amber-600">Vence hoy ({fecha}).</p>
  }
  return (
    <p className="text-sm text-slate-500">
      Vence en {dias} día{dias === 1 ? '' : 's'} ({fecha}).
    </p>
  )
}

function PlanCard({
  plan,
  esElActual,
  cobroAutomaticoActivo,
  procesando,
  onSeleccionar,
}: {
  plan: PlanPublico
  esElActual: boolean
  cobroAutomaticoActivo: boolean
  procesando: boolean
  onSeleccionar: () => void
}) {
  return (
    <div
      className={`flex h-full flex-col gap-3 rounded-2xl border bg-white p-5 shadow-sm ${
        esElActual ? 'border-emerald-300 ring-1 ring-emerald-200' : 'border-slate-200'
      }`}
    >
      <div className="flex items-center gap-2">
        <h3 className="font-semibold text-slate-900">{plan.nombre}</h3>
        {esElActual && (
          <span className="rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-medium text-emerald-700">
            Tu plan actual
          </span>
        )}
      </div>
      <p className="text-slate-900">
        <span className="text-2xl font-bold">${plan.precio.toLocaleString('es-AR')}</span>
        <span className="text-sm text-slate-500"> / {PERIODICIDAD_LABEL[plan.periodicidad]}</span>
      </p>
      <ul className="flex flex-1 flex-col gap-1 text-sm text-slate-600">
        <li>Hasta {plan.limiteRecursos} cancha{plan.limiteRecursos === 1 ? '' : 's'}</li>
        <li>{plan.limiteReservasPorMes} reservas por mes</li>
      </ul>

      {esElActual && cobroAutomaticoActivo ? (
        <p className="text-sm text-emerald-700">✓ Cobro automático activo</p>
      ) : (
        <Button disabled={procesando} onClick={onSeleccionar} className="mt-auto">
          {procesando
            ? 'Redirigiendo…'
            : esElActual
              ? 'Suscribirme con Mercado Pago'
              : 'Cambiar a este plan'}
        </Button>
      )}
    </div>
  )
}

export function MiSuscripcionPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()

  const [suscripcion, setSuscripcion] = useState<MiSuscripcion | null | undefined>(undefined)
  const [planes, setPlanes] = useState<PlanPublico[]>([])
  const [error, setError] = useState<string | null>(null)
  const [vuelvoDeMercadoPago, setVuelvoDeMercadoPago] = useState(false)

  const [procesandoPlanId, setProcesandoPlanId] = useState<string | null>(null)
  const [cancelando, setCancelando] = useState(false)

  function cargarSuscripcion() {
    return obtenerMiSuscripcion()
      .then(setSuscripcion)
      .catch((err) => {
        if (axios.isAxiosError(err) && err.response?.status === 404) {
          setSuscripcion(null)
          return
        }
        setError(extractError(err))
      })
  }

  function cargar() {
    setError(null)
    cargarSuscripcion()
    listarPlanesPublicos()
      .then(setPlanes)
      .catch(() => {})
  }

  useEffect(cargar, [])

  // Mercado Pago vuelve acá después de que el negocio autoriza (o no) el cobro recurrente. La
  // activación llega después, por webhook, así que puede tardar en reflejarse.
  useEffect(() => {
    if (searchParams.get('mp') === 'vuelta') {
      setVuelvoDeMercadoPago(true)
      navigate('/panel/suscripcion', { replace: true })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  // Mientras esperamos que el webhook confirme el primer cobro, la suscripción sigue mostrando el
  // estado viejo (ej. "Cancelada" si se venía de cancelar y volver a suscribir). Sondeamos unas
  // cuantas veces hasta ver que el estado cambió, en vez de dejar la card desactualizada para siempre.
  useEffect(() => {
    if (!vuelvoDeMercadoPago) return

    let cancelado = false
    let intentos = 0

    async function sondear() {
      intentos += 1
      try {
        const actual = await obtenerMiSuscripcion()
        if (cancelado) return
        setSuscripcion(actual)
        if (actual.estado !== EstadoSuscripcion.Cancelada) {
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
  }, [vuelvoDeMercadoPago])

  // Elige/cambia el plan (según si ya había una suscripción asignada) y, si el plan tiene costo y
  // todavía no hay cobro automático activo para él, manda directo a pagarlo con Mercado Pago: un
  // solo click cubre "elegir/cambiar" y "pagar", en vez de dos pasos separados.
  async function handleSeleccionarPlan(plan: PlanPublico) {
    setProcesandoPlanId(plan.id)
    setError(null)
    try {
      let actual: MiSuscripcion | null = suscripcion ?? null
      if (actual === null) {
        actual = await elegirPlan(plan.id)
      } else if (actual.planId !== plan.id) {
        actual = await cambiarPlanMiSuscripcion(plan.id)
      }
      setSuscripcion(actual)

      if (plan.precio > 0 && !actual.cobroAutomaticoActivo) {
        const initPoint = await iniciarSuscripcionMercadoPago()
        window.location.href = initPoint
        return
      }
    } catch (err) {
      setError(extractError(err))
    } finally {
      setProcesandoPlanId(null)
    }
  }

  async function handleCancelar() {
    if (!window.confirm('¿Seguro que querés cancelar tu suscripción? Perdés acceso al finalizar el período vigente.')) {
      return
    }
    setCancelando(true)
    setError(null)
    try {
      await cancelarMiSuscripcion()
      cargar()
    } catch (err) {
      setError(extractError(err))
    } finally {
      setCancelando(false)
    }
  }

  if (suscripcion === undefined) return <Spinner />

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-xl font-semibold text-slate-900">Mi suscripción</h1>

      {vuelvoDeMercadoPago && (
        <div className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
          Estamos confirmando tu suscripción con Mercado Pago. Puede tardar unos minutos en reflejarse acá.
        </div>
      )}
      {error && <ErrorBanner message={error} />}

      {suscripcion !== null && (
        <Card className="flex flex-col gap-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="font-semibold text-slate-900">{suscripcion.planNombre}</p>
              <p className="text-sm text-slate-500">
                ${suscripcion.planPrecio.toLocaleString('es-AR')} / {PERIODICIDAD_LABEL[suscripcion.periodicidad]}
              </p>
            </div>
            <span
              className={`rounded-full px-2 py-0.5 text-xs font-medium ${
                suscripcion.estaActiva ? 'bg-emerald-50 text-emerald-700' : 'bg-red-50 text-red-700'
              }`}
            >
              {ESTADO_LABEL[suscripcion.estado]}
            </span>
          </div>
          <TextoVencimiento suscripcion={suscripcion} />
        </Card>
      )}

      {planes.length > 0 && (
        <div className="flex flex-col gap-3">
          {suscripcion === null && <p className="text-slate-500">Todavía no tenés una suscripción asignada — elegí un plan.</p>}
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {planes.map((plan) => (
              <PlanCard
                key={plan.id}
                plan={plan}
                esElActual={suscripcion !== null && suscripcion.planId === plan.id}
                cobroAutomaticoActivo={
                  suscripcion !== null && suscripcion.planId === plan.id && suscripcion.cobroAutomaticoActivo
                }
                procesando={procesandoPlanId === plan.id}
                onSeleccionar={() => handleSeleccionarPlan(plan)}
              />
            ))}
          </div>
        </div>
      )}

      {suscripcion !== null && (suscripcion.estado !== EstadoSuscripcion.Cancelada || suscripcion.cobroAutomaticoActivo) && (
        <Card className="flex flex-col gap-3">
          <h2 className="font-semibold text-slate-900">Cancelar suscripción</h2>
          <p className="text-sm text-slate-500">
            {suscripcion.cobroAutomaticoActivo
              ? 'También cancela el cobro automático en Mercado Pago: no te va a cobrar de nuevo.'
              : 'Dejás de tener acceso al finalizar el período vigente.'}{' '}
            Podés volver a suscribirte cuando quieras.
          </p>
          <button
            type="button"
            disabled={cancelando}
            onClick={handleCancelar}
            className="self-start rounded-lg border border-red-300 px-3 py-1.5 text-sm font-medium text-red-600 hover:bg-red-50 disabled:opacity-50"
          >
            {cancelando ? 'Cancelando…' : 'Cancelar suscripción'}
          </button>
        </Card>
      )}
    </div>
  )
}
